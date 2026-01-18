using System.Text.Json;
using Microsoft.Extensions.Logging;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.LLM;

namespace VidFlow.Api.Features.Agents.Agents;

/// <summary>
/// The Showrunner agent reviews the entire project for cross-scene consistency,
/// continuity issues, and overall narrative coherence.
/// </summary>
public class ShowrunnerAgent : ICreativeAgent
{
    private readonly ILlmProvider _fallbackProvider;
    private readonly ILogger<ShowrunnerAgent> _logger;

    private const decimal DefaultTemperature = 0.6m;
    private const int DefaultMaxTokens = 2000;

    public AgentRole Role => AgentRole.Showrunner;

    public ShowrunnerAgent(ILlmProvider llmProvider, ILogger<ShowrunnerAgent> logger)
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
            var systemPrompt = @"You are an experienced showrunner reviewing a short film project for consistency and quality.
Focus on cross-scene continuity, character arc consistency, tone coherence, and overall narrative flow.
Your role is to ensure the film works as a cohesive whole, not just individual scenes.";

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

            var issues = ExtractIssues(response.Content);

            var proposal = AgentProposal.Create(
                context.Scene.Id,
                Role,
                issues.Any() ? $"Continuity review: {issues.Count} issue(s) found" : "Continuity review: Scene integrates well",
                response.Content.Length > 800 ? response.Content[..800] + "..." : response.Content,
                0, // Showrunner doesn't change runtime directly
                JsonSerializer.Serialize(new
                {
                    ShowrunnerNotes = response.Content,
                    ContinuityIssues = issues,
                    OverallAssessment = issues.Any() ? "NeedsRevision" : "Approved",
                    IntegrationScore = CalculateIntegrationScore(response.Content)
                }),
                response.TokensUsed,
                response.CostUsd);

            return proposal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Showrunner agent failed to analyze scene {SceneId}", context.Scene.Id);
            return null;
        }
    }

    private static string BuildPrompt(AgentContext context)
    {
        var priorProposalsSummary = context.PriorProposals.Any()
            ? string.Join("\n", context.PriorProposals.Select(p => $"- {p.Role}: {p.Summary}"))
            : "No prior proposals";

        var charactersSummary = context.Characters.Any()
            ? string.Join("\n", context.Characters.Select(c => $"- {c.Name} ({c.Role}): {c.Archetype}"))
            : "No characters defined";

        return $"""
            As the Showrunner, review this scene for cross-project consistency:

            PROJECT CONTEXT:
            Story Bible Themes: {context.StoryBible?.Themes ?? "Not defined"}
            Story Bible Tone: {context.StoryBible?.Tone ?? "Not defined"}
            Visual Style: {context.StoryBible?.VisualStyle ?? "Not defined"}

            CHARACTERS:
            {charactersSummary}

            CURRENT SCENE:
            Scene: {context.Scene.Title} (Scene {context.Scene.Number})
            Location: {context.Scene.Location}
            Time: {context.Scene.TimeOfDay}
            Narrative Goal: {context.Scene.NarrativeGoal}
            Emotional Beat: {context.Scene.EmotionalBeat}
            
            Script:
            {context.Scene.Script}

            Characters in Scene: {string.Join(", ", context.Scene.CharacterNames)}

            AGENT PROPOSALS FOR THIS SCENE:
            {priorProposalsSummary}

            Review for:
            1. CONTINUITY - Does this scene maintain consistency with the story bible?
            2. CHARACTER CONSISTENCY - Are character behaviors consistent with their definitions?
            3. TONE COHERENCE - Does the emotional beat fit the overall project tone?
            4. NARRATIVE FLOW - Does this scene properly advance the story?
            5. VISUAL CONSISTENCY - Does the visual approach match the style guide?

            Identify any issues and rate the scene's integration on a scale of 1-10.
            Be specific about any continuity breaks or inconsistencies found.
            """;
    }

    private static List<string> ExtractIssues(string content)
    {
        var issues = new List<string>();
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var lower = line.ToLowerInvariant();
            if (lower.Contains("issue") || lower.Contains("inconsisten") || 
                lower.Contains("problem") || lower.Contains("concern") ||
                lower.Contains("conflict") || lower.Contains("break"))
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 10 && trimmed.Length < 200)
                    issues.Add(trimmed);
            }
        }

        return issues.Take(5).ToList();
    }

    private static int CalculateIntegrationScore(string content)
    {
        var lower = content.ToLowerInvariant();
        
        // Look for explicit scores
        for (int i = 10; i >= 1; i--)
        {
            if (lower.Contains($"{i}/10") || lower.Contains($"{i} out of 10") || 
                lower.Contains($"score: {i}") || lower.Contains($"rating: {i}"))
                return i;
        }

        // Estimate based on content
        var negativeWords = new[] { "issue", "problem", "inconsistent", "conflict", "concern", "break" };
        var positiveWords = new[] { "consistent", "cohesive", "well-integrated", "excellent", "good" };

        var negCount = negativeWords.Count(w => lower.Contains(w));
        var posCount = positiveWords.Count(w => lower.Contains(w));

        return Math.Clamp(7 + posCount - negCount, 1, 10);
    }
}
