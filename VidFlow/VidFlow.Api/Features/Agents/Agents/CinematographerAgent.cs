using System.Text.Json;
using Microsoft.Extensions.Logging;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.LLM;

namespace VidFlow.Api.Features.Agents.Agents;

public class CinematographerAgent : ICreativeAgent
{
    private readonly ILlmProvider _fallbackProvider;
    private readonly ILogger<CinematographerAgent> _logger;

    private const decimal DefaultTemperature = 0.6m;
    private const int DefaultMaxTokens = 2000;

    public AgentRole Role => AgentRole.Cinematographer;

    public CinematographerAgent(ILlmProvider llmProvider, ILogger<CinematographerAgent> logger)
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
            var systemPrompt = @"You are an expert cinematographer creating shot lists and visual plans for film scenes.
Focus on camera angles, movements, lens choices, lighting, and visual composition.
Your goal is to translate the director's vision into specific, achievable shots.";

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

            var shots = ParseShotsFromResponse(response.Content, context.Scene.Id);

            var proposal = AgentProposal.Create(
                context.Scene.Id,
                Role,
                "Visual storytelling plan with detailed shot list",
                ExtractRationale(response.Content),
                CalculateShotsDuration(shots),
                JsonSerializer.Serialize(new
                {
                    CinematographyNotes = response.Content,
                    SuggestedShots = shots,
                    VisualStyle = context.StoryBible?.VisualStyle ?? "Standard"
                }),
                response.TokensUsed,
                response.CostUsd);

            return proposal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cinematographer agent failed to analyze scene {SceneId}", context.Scene.Id);
            return null;
        }
    }

    private static string BuildPrompt(AgentContext context)
    {
        var existingShots = context.Scene.Shots.Any()
            ? string.Join("\n", context.Scene.Shots.Select(s => $"  Shot {s.Number}: {s.Type} - {s.Duration} - {s.Description}"))
            : "  No shots defined yet";

        return $"""
            Create a detailed shot list for this scene:

            Scene: {context.Scene.Title} (Scene {context.Scene.Number})
            Location: {context.Scene.Location}
            Time of Day: {context.Scene.TimeOfDay}
            Target Runtime: {context.Scene.RuntimeTargetSeconds} seconds

            Script:
            {context.Scene.Script}

            Emotional Beat: {context.Scene.EmotionalBeat}
            Characters: {string.Join(", ", context.Scene.CharacterNames)}

            Visual Style Guidelines:
            {context.StoryBible?.VisualStyle ?? "No specific style defined"}

            Director's Notes:
            {string.Join("\n", context.PriorProposals.Where(p => p.Role == AgentRole.Director).Select(p => p.Summary))}

            Current Shot List:
            {existingShots}

            Create a shot list with:
            1. Shot number and type (Wide, Medium, Close-up, etc.)
            2. Duration estimate in seconds
            3. Camera movement (Static, Pan, Dolly, Handheld, etc.)
            4. Framing and composition notes
            5. Lighting considerations

            Format each shot as:
            SHOT [number]: [type] | [duration]s | [camera] | [description]
            """;
    }

    private static List<object> ParseShotsFromResponse(string content, Guid sceneId)
    {
        var shots = new List<object>();
        var lines = content.Split('\n');
        var shotNumber = 1;

        foreach (var line in lines)
        {
            if (line.Contains("SHOT", StringComparison.OrdinalIgnoreCase) && line.Contains("|"))
            {
                var parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    shots.Add(new
                    {
                        Number = shotNumber++,
                        Type = parts[0].Replace("SHOT", "").Trim().TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ' '),
                        Duration = parts[1].Trim(),
                        Camera = parts.Length > 2 ? parts[2].Trim() : "Standard",
                        Description = parts.Length > 3 ? parts[3].Trim() : ""
                    });
                }
            }
        }

        if (!shots.Any())
        {
            shots.Add(new { Number = 1, Type = "Wide Shot", Duration = "5s", Camera = "Static", Description = "Establishing shot" });
            shots.Add(new { Number = 2, Type = "Medium Shot", Duration = "8s", Camera = "Slight push", Description = "Character focus" });
        }

        return shots;
    }

    private static string ExtractRationale(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !l.StartsWith("SHOT", StringComparison.OrdinalIgnoreCase))
            .Take(3);
        return lines.Any() ? string.Join(" ", lines).Trim() : "Visual plan to enhance storytelling through careful shot selection.";
    }

    private static int CalculateShotsDuration(List<object> shots)
    {
        return shots.Count * 3;
    }
}
