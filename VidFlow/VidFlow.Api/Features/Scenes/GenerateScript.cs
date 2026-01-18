using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Features.LLM;

namespace VidFlow.Api.Features.Scenes;

public static class GenerateScript
{
    public record Request(string? AdditionalDirection = null);

    public record Response(
        Guid SceneId,
        string GeneratedScript,
        int TokensUsed,
        decimal CostUsd);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/scenes/{sceneId}/generate-script", Handler)
           .WithName("GenerateScript")
           .WithTags("Scenes");
    }

    private static async Task<IResult> Handler(
        Guid sceneId,
        Request request,
        VidFlowDbContext db,
        ILlmProvider llmProvider,
        CancellationToken ct)
    {
        var scene = await db.Scenes
            .Include(s => s.Shots)
            .FirstOrDefaultAsync(s => s.Id == sceneId, ct);

        if (scene is null)
            return Results.NotFound($"Scene {sceneId} not found");

        var project = await db.Projects.FindAsync([scene.ProjectId], ct);
        var storyBible = await db.StoryBibles.FirstOrDefaultAsync(sb => sb.ProjectId == scene.ProjectId, ct);
        var characters = await db.Characters
            .Where(c => c.ProjectId == scene.ProjectId && scene.CharacterNames.Contains(c.Name))
            .ToListAsync(ct);

        var prompt = BuildPrompt(scene, project, storyBible, characters, request.AdditionalDirection);
        var systemPrompt = @"You are an expert screenwriter generating scripts for short films.
Write in proper screenplay format with scene headings, action lines, and dialogue.
Ensure character voices are distinct and consistent with their defined personalities.
Keep the script tight and focused on the scene's narrative goal.";

        var response = await llmProvider.CompleteAsync(new LlmRequest
        {
            Prompt = prompt,
            SystemPrompt = systemPrompt,
            Temperature = 0.8m,
            MaxTokens = 3000,
            Model = ""
        }, ct);

        return Results.Ok(new Response(
            sceneId,
            response.Content,
            response.TokensUsed,
            response.CostUsd));
    }

    private static string BuildPrompt(
        Domain.Entities.Scene scene,
        Domain.Entities.Project? project,
        Domain.Entities.StoryBible? storyBible,
        List<Domain.Entities.Character> characters,
        string? additionalDirection)
    {
        var characterDetails = characters.Any()
            ? string.Join("\n", characters.Select(c => $"""
                {c.Name.ToUpper()}:
                - Role: {c.Role}
                - Archetype: {c.Archetype}
                - Traits: {string.Join(", ", c.Traits)}
                - Voice: {c.Description}
                """))
            : "No character details available";

        var shotList = scene.Shots.Any()
            ? string.Join("\n", scene.Shots.OrderBy(s => s.Number).Select(s => 
                $"Shot {s.Number}: {s.Type} ({s.Duration}) - {s.Description}"))
            : "No shots defined";

        return $"""
            Generate a screenplay for this scene:

            PROJECT: {project?.Title ?? "Untitled"}
            LOGLINE: {project?.Logline ?? "Not specified"}

            SCENE HEADING:
            INT/EXT. {scene.Location.ToUpper()} - {scene.TimeOfDay.ToUpper()}

            SCENE DETAILS:
            - Title: {scene.Title}
            - Scene Number: {scene.Number}
            - Narrative Goal: {scene.NarrativeGoal}
            - Emotional Beat: {scene.EmotionalBeat}
            - Target Runtime: {scene.RuntimeTargetSeconds} seconds (approximately {scene.RuntimeTargetSeconds / 60} page)

            CHARACTERS IN SCENE:
            {characterDetails}

            STORY BIBLE CONTEXT:
            - Themes: {storyBible?.Themes ?? "Not defined"}
            - Tone: {storyBible?.Tone ?? "Not defined"}
            - Pacing: {storyBible?.PacingRules ?? "Not defined"}

            SHOT LIST (for visual reference):
            {shotList}

            {(string.IsNullOrEmpty(additionalDirection) ? "" : $"ADDITIONAL DIRECTION:\n{additionalDirection}")}

            Write the screenplay in standard format:
            - Scene heading (already provided above)
            - Action/description lines
            - Character names in CAPS before dialogue
            - Dialogue with parentheticals where needed
            - Transitions if appropriate

            Make the dialogue feel natural and true to each character's voice.
            Ensure the scene achieves its narrative goal and emotional beat.
            """;
    }
}
