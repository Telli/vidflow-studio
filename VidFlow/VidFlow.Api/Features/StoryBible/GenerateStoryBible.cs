using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Features.LLM;

namespace VidFlow.Api.Features.StoryBible;

public static class GenerateStoryBible
{
    public record Request(string? AdditionalContext = null);

    public record Response(
        Guid Id,
        Guid ProjectId,
        string Themes,
        string WorldRules,
        string Tone,
        string VisualStyle,
        string PacingRules,
        int Version,
        DateTime CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId}/story-bible/generate", Handler)
           .WithName("GenerateStoryBible")
           .WithTags("StoryBible");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        Request request,
        VidFlowDbContext db,
        ILlmProvider llmProvider,
        CancellationToken ct)
    {
        var project = await db.Projects
            .Include(p => p.StoryBible)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        if (project is null)
            return Results.NotFound($"Project {projectId} not found");

        var prompt = BuildPrompt(project.Title, project.Logline, project.RuntimeTargetSeconds, request.AdditionalContext);
        var systemPrompt = @"You are an expert story consultant helping filmmakers develop their creative vision.
Generate a comprehensive story bible that will guide all creative decisions for a short film.
Be specific, evocative, and practical. Your output should inspire and constrain in equal measure.";

        var llmResponse = await llmProvider.CompleteAsync(new LlmRequest
        {
            Prompt = prompt,
            SystemPrompt = systemPrompt,
            Temperature = 0.8m,
            MaxTokens = 2500,
            Model = "gpt-3.5-turbo"
        }, ct);

        var parsed = ParseStoryBibleResponse(llmResponse.Content);

        Domain.Entities.StoryBible storyBible;
        
        if (project.StoryBible is not null)
        {
            // Update existing story bible
            project.StoryBible.Update(
                parsed.Themes,
                parsed.WorldRules,
                parsed.Tone,
                parsed.VisualStyle,
                parsed.PacingRules);
            storyBible = project.StoryBible;
        }
        else
        {
            // Create new story bible
            storyBible = Domain.Entities.StoryBible.Create(
                projectId,
                parsed.Themes,
                parsed.WorldRules,
                parsed.Tone,
                parsed.VisualStyle,
                parsed.PacingRules);
            db.StoryBibles.Add(storyBible);
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(new Response(
            storyBible.Id,
            storyBible.ProjectId,
            storyBible.Themes,
            storyBible.WorldRules,
            storyBible.Tone,
            storyBible.VisualStyle,
            storyBible.PacingRules,
            storyBible.Version,
            storyBible.CreatedAt));
    }

    private static string BuildPrompt(string title, string logline, int runtimeSeconds, string? additionalContext)
    {
        return $"""
            Generate a story bible for the following short film:

            Title: {title}
            Logline: {logline}
            Target Runtime: {runtimeSeconds / 60} minutes

            {(string.IsNullOrEmpty(additionalContext) ? "" : $"Additional Context: {additionalContext}")}

            Create a comprehensive story bible with these sections:

            1. THEMES (2-3 core themes the story explores)
            2. WORLD RULES (the logic and constraints of the story world)
            3. TONE (the emotional and stylistic feel)
            4. VISUAL STYLE (cinematography, color palette, visual motifs)
            5. PACING RULES (rhythm, tension building, scene length guidelines)

            Format your response exactly as:
            THEMES:
            [your themes here]

            WORLD_RULES:
            [your world rules here]

            TONE:
            [your tone description here]

            VISUAL_STYLE:
            [your visual style here]

            PACING_RULES:
            [your pacing rules here]
            """;
    }

    private static (string Themes, string WorldRules, string Tone, string VisualStyle, string PacingRules) ParseStoryBibleResponse(string content)
    {
        var sections = new Dictionary<string, string>
        {
            ["THEMES"] = "",
            ["WORLD_RULES"] = "",
            ["TONE"] = "",
            ["VISUAL_STYLE"] = "",
            ["PACING_RULES"] = ""
        };

        var currentSection = "";
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("THEMES", StringComparison.OrdinalIgnoreCase))
                currentSection = "THEMES";
            else if (trimmed.StartsWith("WORLD_RULES", StringComparison.OrdinalIgnoreCase) || 
                     trimmed.StartsWith("WORLD RULES", StringComparison.OrdinalIgnoreCase))
                currentSection = "WORLD_RULES";
            else if (trimmed.StartsWith("TONE", StringComparison.OrdinalIgnoreCase))
                currentSection = "TONE";
            else if (trimmed.StartsWith("VISUAL_STYLE", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.StartsWith("VISUAL STYLE", StringComparison.OrdinalIgnoreCase))
                currentSection = "VISUAL_STYLE";
            else if (trimmed.StartsWith("PACING_RULES", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.StartsWith("PACING RULES", StringComparison.OrdinalIgnoreCase))
                currentSection = "PACING_RULES";
            else if (!string.IsNullOrEmpty(currentSection) && !string.IsNullOrWhiteSpace(trimmed) && !trimmed.EndsWith(":"))
            {
                sections[currentSection] += (sections[currentSection].Length > 0 ? "\n" : "") + trimmed;
            }
        }

        return (
            sections["THEMES"].Trim(),
            sections["WORLD_RULES"].Trim(),
            sections["TONE"].Trim(),
            sections["VISUAL_STYLE"].Trim(),
            sections["PACING_RULES"].Trim()
        );
    }
}
