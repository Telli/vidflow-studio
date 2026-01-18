using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.Exceptions;
using VidFlow.Api.Features.LLM;
using VidFlow.Api.Hubs;

namespace VidFlow.Api.Features.Agents;

public class AgentRunner
{
    private readonly IEnumerable<ICreativeAgent> _agents;
    private readonly VidFlowDbContext _db;
    private readonly IHubContext<AgentActivityHub> _hub;
    private readonly ITrackedLlmProviderFactory _llmProviderFactory;
    private readonly LlmSettings _llmSettings;

    public AgentRunner(
        IEnumerable<ICreativeAgent> agents,
        VidFlowDbContext db,
        IHubContext<AgentActivityHub> hub,
        ITrackedLlmProviderFactory llmProviderFactory,
        IOptions<LlmSettings> llmSettings)
    {
        _agents = agents.OrderBy(a => GetAgentOrder(a.Role));
        _db = db;
        _hub = hub;
        _llmProviderFactory = llmProviderFactory;
        _llmSettings = llmSettings.Value;
    }

    public async Task<AgentPipelineResult> RunPipelineAsync(Guid sceneId, CancellationToken ct, string? jobId = null)
    {
        var scene = await _db.Scenes
            .Include(s => s.Shots)
            .Include(s => s.Proposals)
            .FirstOrDefaultAsync(s => s.Id == sceneId, ct);

        if (scene is null || scene.Status != SceneStatus.Draft)
            return AgentPipelineResult.Failed(AgentRole.Writer, "Scene not found or not in Draft status");

        // Try to acquire lock for pipeline execution (5 minute timeout)
        if (!scene.TryAcquireLock("AgentPipeline", TimeSpan.FromMinutes(5)))
            return AgentPipelineResult.Failed(AgentRole.Writer, $"Scene is locked by {scene.LockedBy} until {scene.LockedUntil}");

        await _db.SaveChangesAsync(ct);
        await _hub.Clients.Group($"scene-{sceneId}").SendAsync("SceneLocked", sceneId, "AgentPipeline", ct);

        try
        {
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == scene.ProjectId, ct);
            if (project is null)
                return AgentPipelineResult.Failed(AgentRole.Writer, "Project not found");

            // Sync budget from persisted proposals
            var totalPersistedCost = await _db.AgentProposals
                .Where(p => p.Scene.ProjectId == scene.ProjectId)
                .SumAsync(p => p.CostUsd, ct);

            if (totalPersistedCost > project.CurrentSpendUsd)
            {
                project.AddSpend(totalPersistedCost - project.CurrentSpendUsd);
                await _db.SaveChangesAsync(ct);
            }

            var storyBible = await _db.StoryBibles.FirstOrDefaultAsync(sb => sb.ProjectId == scene.ProjectId, ct);
            var characters = await _db.Characters.Where(c => c.ProjectId == scene.ProjectId).ToListAsync(ct);
            var proposals = new List<AgentProposal>();

            foreach (var agent in _agents)
            {
                var maxEstimatedCost = EstimateMaxAgentCallCostUsd(agent.Role);
                if (project.WouldExceedBudget(maxEstimatedCost))
                {
                    var budgetError = $"Budget exceeded: current spend ${project.CurrentSpendUsd:F4}, cap ${project.BudgetCapUsd:F4}, estimated cost ${maxEstimatedCost:F4}";
                    return AgentPipelineResult.Failed(agent.Role, budgetError);
                }

                await _hub.Clients.Group($"scene-{sceneId}").SendAsync("AgentStarted", sceneId, agent.Role.ToString(), ct);

                try
                {
                    // Get role-specific config overrides
                    var roleKey = agent.Role.ToString();
                    _llmSettings.RoleOverrides.TryGetValue(roleKey, out var roleConfig);
                    var llmConfig = AgentLlmConfig.FromRoleOverride(roleConfig);

                    // Create tracked provider with context for this agent
                    var interactionContext = LlmInteractionContext.ForAgentRun(
                        project.Id,
                        scene.Id,
                        agent.Role,
                        jobId);

                    var trackedProvider = _llmProviderFactory.CreateTracked(
                        llmConfig.Provider,
                        interactionContext);

                    // Build context with LLM provider
                    var context = new AgentContext(scene, storyBible, characters, proposals)
                    {
                        LlmProvider = trackedProvider,
                        LlmConfig = llmConfig,
                        JobId = jobId
                    };

                    var proposal = await agent.AnalyzeAsync(context, ct);

                    if (proposal is not null)
                    {
                        if (project.WouldExceedBudget(proposal.CostUsd))
                        {
                            var budgetError = $"Budget exceeded: current spend ${project.CurrentSpendUsd:F4}, cap ${project.BudgetCapUsd:F4}, proposal cost ${proposal.CostUsd:F4}";
                            return AgentPipelineResult.Failed(agent.Role, budgetError);
                        }

                        proposals.Add(proposal);
                        _db.AgentProposals.Add(proposal);
                        project.AddSpend(proposal.CostUsd);
                        await _db.SaveChangesAsync(ct);
                        await _hub.Clients.Group($"scene-{sceneId}").SendAsync("ProposalCreated", sceneId, proposal, ct);
                    }

                    await _hub.Clients.Group($"scene-{sceneId}").SendAsync("AgentCompleted", sceneId, agent.Role.ToString(), ct);
                }
                catch (Exception ex)
                {
                    await _hub.Clients.Group($"scene-{sceneId}").SendAsync("AgentFailed", sceneId, agent.Role.ToString(), ex.Message, ct);
                    return AgentPipelineResult.Failed(agent.Role, ex.Message);
                }
            }

            await _db.SaveChangesAsync(ct);
            return AgentPipelineResult.Succeeded(proposals);
        }
        finally
        {
            // Always release lock when done
            scene.ReleaseLock();
            await _db.SaveChangesAsync(ct);
            await _hub.Clients.Group($"scene-{sceneId}").SendAsync("SceneUnlocked", sceneId, ct);
        }
    }

    private static int GetAgentOrder(AgentRole role) => role switch
    {
        AgentRole.Writer => 1,
        AgentRole.Director => 2,
        AgentRole.Cinematographer => 3,
        AgentRole.Editor => 4,
        AgentRole.Producer => 5,
        AgentRole.Showrunner => 6,
        _ => 99
    };

    private decimal EstimateMaxAgentCallCostUsd(AgentRole role)
    {
        // Get cost per token from the default provider
        var costPerToken = _llmSettings.DefaultProvider.ToLowerInvariant() switch
        {
            "anthropic" => _llmSettings.Anthropic.CostPerOutputToken,
            "gemini" => _llmSettings.Gemini.CostPerOutputToken,
            _ => _llmSettings.OpenAi.CostPerOutputToken
        };

        var maxTokens = role switch
        {
            AgentRole.Writer => 1500,
            AgentRole.Director => 1500,
            AgentRole.Cinematographer => 2000,
            AgentRole.Editor => 1500,
            AgentRole.Producer => 1000,
            AgentRole.Showrunner => 2000,
            _ => 1500
        };

        const int promptOverheadTokens = 1000;
        return (maxTokens + promptOverheadTokens) * costPerToken;
    }
}
