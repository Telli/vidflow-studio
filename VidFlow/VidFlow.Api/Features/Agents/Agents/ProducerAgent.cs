using System.Text.Json;
using Microsoft.Extensions.Logging;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.LLM;

namespace VidFlow.Api.Features.Agents.Agents;

public class ProducerAgent : ICreativeAgent
{
    private readonly ILlmProvider _fallbackProvider;
    private readonly ILogger<ProducerAgent> _logger;

    private const decimal DefaultTemperature = 0.4m;
    private const int DefaultMaxTokens = 1000;

    public AgentRole Role => AgentRole.Producer;

    public ProducerAgent(ILlmProvider llmProvider, ILogger<ProducerAgent> logger)
    {
        _fallbackProvider = llmProvider;
        _logger = logger;
    }

    public async Task<AgentProposal?> AnalyzeAsync(AgentContext context, CancellationToken ct)
    {
        try
        {
            var provider = context.LlmProvider ?? _fallbackProvider;

            // First, do constraint checks (these don't need LLM)
            var constraintResult = CheckConstraints(context);

            // Then get LLM analysis for production feasibility
            var prompt = BuildPrompt(context, constraintResult);
            var systemPrompt = @"You are an experienced film producer reviewing scenes for production feasibility.
Focus on budget implications, resource requirements, scheduling, and practical constraints.
Your role is to ensure the creative vision can be achieved within production constraints.";

            var temperature = context.LlmConfig?.Temperature ?? DefaultTemperature;
            var maxTokens = context.LlmConfig?.MaxTokens ?? DefaultMaxTokens;
            var model = context.LlmConfig?.Model ?? "";

            var response = await provider.CompleteAsync(new LlmRequest
            {
                Prompt = prompt,
                SystemPrompt = systemPrompt,
                Temperature = temperature,
                MaxTokens = maxTokens,
                Model = model
            }, ct);

            var finalStatus = constraintResult.HasViolations ? "Violations" : "Compliant";
            var summary = constraintResult.HasViolations 
                ? $"Production constraints violated: {string.Join(", ", constraintResult.Issues)}"
                : "Scene meets all production constraints";

            var proposal = AgentProposal.Create(
                context.Scene.Id,
                Role,
                summary,
                response.Content.Length > 500 ? response.Content[..500] + "..." : response.Content,
                0, // Producer doesn't change runtime
                JsonSerializer.Serialize(new
                {
                    ProducerNotes = response.Content,
                    ConstraintsChecked = constraintResult.CheckedConstraints,
                    Status = finalStatus,
                    Issues = constraintResult.Issues,
                    RuntimeAnalysis = new
                    {
                        TargetSeconds = context.Scene.RuntimeTargetSeconds,
                        ProposedImpact = constraintResult.TotalRuntimeImpact,
                        WithinBudget = !constraintResult.RuntimeExceeded
                    },
                    CostAnalysis = new
                    {
                        TotalAgentCost = constraintResult.TotalCost,
                        CostPerAgent = context.PriorProposals.Select(p => new { p.Role, p.CostUsd })
                    },
                    ReadyForApproval = !constraintResult.HasViolations
                }),
                response.TokensUsed,
                response.CostUsd);

            return proposal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Producer agent failed to analyze scene {SceneId}", context.Scene.Id);
            return null;
        }
    }

    private static ConstraintCheckResult CheckConstraints(AgentContext context)
    {
        var issues = new List<string>();
        var checkedConstraints = new List<string> { "Runtime", "Budget", "Resources", "Continuity" };

        var totalRuntimeImpact = context.PriorProposals.Sum(p => p.RuntimeImpactSeconds);
        var projectedRuntime = context.Scene.RuntimeTargetSeconds + totalRuntimeImpact;
        var runtimeExceeded = projectedRuntime > context.Scene.RuntimeTargetSeconds * 1.25;

        if (runtimeExceeded)
        {
            issues.Add($"Runtime exceeds target by {projectedRuntime - context.Scene.RuntimeTargetSeconds}s ({(projectedRuntime * 100 / context.Scene.RuntimeTargetSeconds) - 100}% over)");
        }

        var totalCost = context.PriorProposals.Sum(p => p.CostUsd);
        if (totalCost > 0.10m) // Arbitrary threshold for demo
        {
            issues.Add($"Agent processing cost (${totalCost:F4}) exceeds recommended limit");
        }

        var shotCount = context.Scene.Shots.Count;
        if (shotCount > 20)
        {
            issues.Add($"High shot count ({shotCount}) may impact production schedule");
        }

        return new ConstraintCheckResult(
            checkedConstraints,
            issues,
            totalRuntimeImpact,
            runtimeExceeded,
            totalCost,
            issues.Any());
    }

    private static string BuildPrompt(AgentContext context, ConstraintCheckResult constraints)
    {
        return $"""
            Review this scene for production feasibility:

            Scene: {context.Scene.Title} (Scene {context.Scene.Number})
            Location: {context.Scene.Location}
            Time of Day: {context.Scene.TimeOfDay}
            Target Runtime: {context.Scene.RuntimeTargetSeconds} seconds

            Characters Required: {string.Join(", ", context.Scene.CharacterNames)}
            Shot Count: {context.Scene.Shots.Count}

            Constraint Check Results:
            - Runtime Impact: {constraints.TotalRuntimeImpact}s ({(constraints.RuntimeExceeded ? "EXCEEDS" : "within")} target)
            - Agent Processing Cost: ${constraints.TotalCost:F4}
            - Issues Found: {(constraints.Issues.Any() ? string.Join("; ", constraints.Issues) : "None")}

            Previous Agent Proposals:
            {string.Join("\n", context.PriorProposals.Select(p => $"- {p.Role}: {p.Summary} (runtime impact: {p.RuntimeImpactSeconds}s, cost: ${p.CostUsd:F4})"))}

            Provide a brief production assessment:
            1. Overall feasibility rating (1-10)
            2. Key production concerns
            3. Resource requirements
            4. Recommendations for staying on budget/schedule
            5. Final verdict: Ready for approval or needs revision?
            """;
    }

    private record ConstraintCheckResult(
        List<string> CheckedConstraints,
        List<string> Issues,
        int TotalRuntimeImpact,
        bool RuntimeExceeded,
        decimal TotalCost,
        bool HasViolations);
}
