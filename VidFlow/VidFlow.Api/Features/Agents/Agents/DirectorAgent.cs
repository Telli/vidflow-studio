using System.Text.Json;
using Microsoft.Extensions.Logging;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.LLM;

namespace VidFlow.Api.Features.Agents.Agents;

public class DirectorAgent : ICreativeAgent
{
    private readonly ILlmProvider _fallbackProvider;
    private readonly ILogger<DirectorAgent> _logger;

    private const decimal DefaultTemperature = 0.7m;
    private const int DefaultMaxTokens = 1500;

    public AgentRole Role => AgentRole.Director;

    public DirectorAgent(ILlmProvider llmProvider, ILogger<DirectorAgent> logger)
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
            var systemPrompt = @"You are an experienced film director providing creative direction for scenes.
Focus on emotional pacing, dramatic tension, actor blocking, and overall scene vision.
Your role is to shape the emotional journey and ensure the scene serves the narrative.";

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

            var proposal = AgentProposal.Create(
                context.Scene.Id,
                Role,
                "Director's vision for scene pacing and emotional arc",
                ExtractRationale(response.Content),
                EstimateRuntimeImpact(response.Content),
                JsonSerializer.Serialize(new
                {
                    DirectorNotes = response.Content,
                    EmotionalBeat = context.Scene.EmotionalBeat + " (refined)",
                    NarrativeGoal = context.Scene.NarrativeGoal + " (clarified)"
                }),
                response.TokensUsed,
                response.CostUsd);

            return proposal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Director agent failed to analyze scene {SceneId}", context.Scene.Id);
            return null;
        }
    }

    private static string BuildPrompt(AgentContext context)
    {
        return $"""
            As the director, analyze this scene and provide your creative vision:

            Scene: {context.Scene.Title} (Scene {context.Scene.Number})
            Location: {context.Scene.Location}
            Time: {context.Scene.TimeOfDay}
            
            Current Script:
            {context.Scene.Script}

            Narrative Goal: {context.Scene.NarrativeGoal}
            Emotional Beat: {context.Scene.EmotionalBeat}
            Target Runtime: {context.Scene.RuntimeTargetSeconds} seconds

            Characters: {string.Join(", ", context.Scene.CharacterNames)}

            Story Bible Context:
            Tone: {context.StoryBible?.Tone ?? "Not specified"}
            Visual Style: {context.StoryBible?.VisualStyle ?? "Not specified"}
            Pacing Rules: {context.StoryBible?.PacingRules ?? "Not specified"}

            Writer's Input:
            {string.Join("\n", context.PriorProposals.Where(p => p.Role == AgentRole.Writer).Select(p => p.Summary))}

            Provide direction on:
            1. Emotional pacing - how to build and release tension
            2. Key dramatic moments to emphasize
            3. Actor blocking suggestions
            4. Scene rhythm and tempo
            5. Any adjustments to achieve the narrative goal

            Be specific and actionable in your direction.
            """;
    }

    private static string ExtractRationale(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.Length > 0 ? string.Join(" ", lines.Take(3)).Trim() : "Director's creative guidance for scene improvement.";
    }

    private static int EstimateRuntimeImpact(string content)
    {
        if (content.Contains("extend", StringComparison.OrdinalIgnoreCase) || 
            content.Contains("longer", StringComparison.OrdinalIgnoreCase))
            return 10;
        if (content.Contains("trim", StringComparison.OrdinalIgnoreCase) || 
            content.Contains("shorten", StringComparison.OrdinalIgnoreCase))
            return -5;
        return 0;
    }
}
