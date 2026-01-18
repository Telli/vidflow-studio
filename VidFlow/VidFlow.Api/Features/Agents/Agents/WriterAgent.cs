using System.Text.Json;
using Microsoft.Extensions.Logging;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.LLM;

namespace VidFlow.Api.Features.Agents.Agents;

public class WriterAgent : ICreativeAgent
{
    private readonly ILlmProvider _fallbackProvider;
    private readonly ILogger<WriterAgent> _logger;

    private const decimal DefaultTemperature = 0.7m;
    private const int DefaultMaxTokens = 1500;

    public AgentRole Role => AgentRole.Writer;

    public WriterAgent(ILlmProvider llmProvider, ILogger<WriterAgent> logger)
    {
        _fallbackProvider = llmProvider;
        _logger = logger;
    }

    public async Task<AgentProposal?> AnalyzeAsync(AgentContext context, CancellationToken ct)
    {
        try
        {
            // Use provider from context (tracked) or fall back to injected provider
            var provider = context.LlmProvider ?? _fallbackProvider;

            var prompt = BuildPrompt(context);
            var systemPrompt = "You are a professional screenwriter helping to improve scenes for a short film. Focus on dialogue, narrative flow, and character voice.";

            // Apply config overrides
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
                "Enhance dialogue and narrative flow",
                "AI analysis suggests improvements to dialogue, character voice, and narrative progression for better storytelling.",
                15,
                JsonSerializer.Serialize(new
                {
                    Script = response.Content,
                    NarrativeGoal = context.Scene.NarrativeGoal + " (enhanced for clarity)",
                    EmotionalBeat = context.Scene.EmotionalBeat + " (strengthened)"
                }),
                response.TokensUsed,
                response.CostUsd);

            return proposal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Writer agent failed to analyze scene {SceneId}", context.Scene.Id);
            return null;
        }
    }

    private static string BuildPrompt(AgentContext context)
    {
        return $"""
            Analyze this scene and suggest improvements for dialogue and narrative flow:

            Scene Details:
            - Title: {context.Scene.Title}
            - Current Script: {context.Scene.Script}
            - Narrative Goal: {context.Scene.NarrativeGoal}
            - Emotional Beat: {context.Scene.EmotionalBeat}
            - Location: {context.Scene.Location}
            - Time of Day: {context.Scene.TimeOfDay}
            - Characters: {string.Join(", ", context.Scene.CharacterNames)}

            Story Bible Context:
            {context.StoryBible?.Themes ?? "No story bible available"}

            Characters in Scene:
            {string.Join("\n", context.Characters.Select(c => $"- {c.Name}: {c.Role} ({c.Archetype})"))}

            Previous Agent Proposals:
            {string.Join("\n", context.PriorProposals.Select(p => $"- {p.Role}: {p.Summary}"))}

            Please provide specific suggestions for:
            1. Improving dialogue authenticity
            2. Strengthening narrative flow
            3. Enhancing character voice consistency
            4. Adding emotional depth where needed

            Return your analysis in JSON format with the improved script and specific changes.
            """;
    }
}
