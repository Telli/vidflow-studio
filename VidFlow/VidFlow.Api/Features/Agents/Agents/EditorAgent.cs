using System.Text.Json;
using Microsoft.Extensions.Logging;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.LLM;

namespace VidFlow.Api.Features.Agents.Agents;

public class EditorAgent : ICreativeAgent
{
    private readonly ILlmProvider _fallbackProvider;
    private readonly ILogger<EditorAgent> _logger;

    private const decimal DefaultTemperature = 0.5m;
    private const int DefaultMaxTokens = 1500;

    public AgentRole Role => AgentRole.Editor;

    public EditorAgent(ILlmProvider llmProvider, ILogger<EditorAgent> logger)
    {
        _fallbackProvider = llmProvider;
        _logger = logger;
    }

    public async Task<AgentProposal?> AnalyzeAsync(AgentContext context, CancellationToken ct)
    {
        try
        {
            var provider = context.LlmProvider ?? _fallbackProvider;

            var prompt = BuildPrompt(context);
            var systemPrompt = @"You are an expert film editor analyzing scenes for pacing, rhythm, and flow.
Focus on cutting points, scene transitions, timing, and overall narrative momentum.
Your goal is to ensure the scene maintains audience engagement and serves the story efficiently.";

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

            var runtimeImpact = CalculateRuntimeImpact(response.Content, context.Scene.RuntimeTargetSeconds);

            var proposal = AgentProposal.Create(
                context.Scene.Id,
                Role,
                "Editing notes for pacing and rhythm optimization",
                ExtractRationale(response.Content),
                runtimeImpact,
                JsonSerializer.Serialize(new
                {
                    EditorNotes = response.Content,
                    PacingAdjustments = ExtractPacingNotes(response.Content),
                    SuggestedCuts = ExtractCuts(response.Content),
                    RuntimeAdjustment = runtimeImpact
                }),
                response.TokensUsed,
                response.CostUsd);

            return proposal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Editor agent failed to analyze scene {SceneId}", context.Scene.Id);
            return null;
        }
    }

    private static string BuildPrompt(AgentContext context)
    {
        var shotList = context.Scene.Shots.Any()
            ? string.Join("\n", context.Scene.Shots.Select(s => $"  Shot {s.Number}: {s.Type} ({s.Duration}) - {s.Description}"))
            : "  No shots defined";

        var totalShotDuration = context.Scene.Shots.Sum(s => s.GetDurationSeconds());

        return $"""
            Analyze this scene for editing and pacing improvements:

            Scene: {context.Scene.Title} (Scene {context.Scene.Number})
            Target Runtime: {context.Scene.RuntimeTargetSeconds} seconds
            Current Shot Duration Total: {totalShotDuration} seconds

            Script:
            {context.Scene.Script}

            Shot List:
            {shotList}

            Emotional Beat: {context.Scene.EmotionalBeat}
            Narrative Goal: {context.Scene.NarrativeGoal}

            Pacing Rules from Story Bible:
            {context.StoryBible?.PacingRules ?? "No specific pacing rules defined"}

            Previous Agent Suggestions:
            {string.Join("\n", context.PriorProposals.Select(p => $"- {p.Role}: {p.Summary}"))}

            Analyze and suggest:
            1. Pacing adjustments - where to speed up or slow down
            2. Potential cuts to tighten the scene
            3. Shot order optimizations
            4. Transition suggestions between shots
            5. Runtime optimization to hit target

            Estimate how many seconds can be trimmed or should be added.
            Format suggestions clearly with specific timings.
            """;
    }

    private static int CalculateRuntimeImpact(string content, int targetRuntime)
    {
        var lowerContent = content.ToLowerInvariant();
        
        if (lowerContent.Contains("trim") || lowerContent.Contains("cut") || lowerContent.Contains("reduce"))
        {
            if (lowerContent.Contains("significantly") || lowerContent.Contains("heavily"))
                return -15;
            return -8;
        }
        
        if (lowerContent.Contains("extend") || lowerContent.Contains("add") || lowerContent.Contains("expand"))
        {
            return 5;
        }
        
        return -3; // Default slight trim
    }

    private static string ExtractRationale(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(3);
        return lines.Any() ? string.Join(" ", lines).Trim() : "Editing suggestions to optimize scene pacing and flow.";
    }

    private static List<string> ExtractPacingNotes(string content)
    {
        return content.Split('\n')
            .Where(l => l.Contains("pacing", StringComparison.OrdinalIgnoreCase) || 
                       l.Contains("rhythm", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();
    }

    private static List<string> ExtractCuts(string content)
    {
        return content.Split('\n')
            .Where(l => l.Contains("cut", StringComparison.OrdinalIgnoreCase) || 
                       l.Contains("trim", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();
    }
}
