using System.Text;
using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;

namespace VidFlow.Api.Features.Export;

public static class ExportFountain
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId}/export/fountain", Handler)
           .WithName("ExportFountain")
           .WithTags("Export")
           .Produces<string>(200, "text/plain");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var project = await db.Projects
            .Include(p => p.Scenes)
            .Include(p => p.Characters)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        if (project is null)
            return Results.NotFound($"Project {projectId} not found");

        var scenes = project.Scenes.OrderBy(s => s.Number).ToList();
        var fountain = GenerateFountainScript(project, scenes);

        return Results.Text(fountain, "text/plain");
    }

    private static string GenerateFountainScript(
        Domain.Entities.Project project,
        List<Domain.Entities.Scene> scenes)
    {
        var sb = new StringBuilder();

        // Title page
        sb.AppendLine($"Title: {project.Title}");
        sb.AppendLine($"Credit: Written by");
        sb.AppendLine($"Author: VidFlow Studio");
        sb.AppendLine($"Draft date: {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("===");
        sb.AppendLine();

        // Logline as opening note
        if (!string.IsNullOrEmpty(project.Logline))
        {
            sb.AppendLine($"/* {project.Logline} */");
            sb.AppendLine();
        }

        foreach (var scene in scenes)
        {
            // Scene heading
            var intExt = scene.Location.ToUpperInvariant().Contains("INT") ? "" : 
                         scene.Location.ToUpperInvariant().Contains("EXT") ? "" : "INT. ";
            sb.AppendLine($"{intExt}{scene.Location.ToUpperInvariant()} - {scene.TimeOfDay.ToUpperInvariant()}");
            sb.AppendLine();

            // Scene title as action/description
            if (!string.IsNullOrEmpty(scene.Title))
            {
                sb.AppendLine($"/* Scene {scene.Number}: {scene.Title} */");
                sb.AppendLine($"/* Narrative Goal: {scene.NarrativeGoal} */");
                sb.AppendLine($"/* Emotional Beat: {scene.EmotionalBeat} */");
                sb.AppendLine();
            }

            // Script content
            if (!string.IsNullOrEmpty(scene.Script))
            {
                sb.AppendLine(scene.Script);
                sb.AppendLine();
            }
            else
            {
                // Generate placeholder based on scene metadata
                sb.AppendLine($"[Scene {scene.Number} - {scene.Title}]");
                sb.AppendLine();
                if (scene.CharacterNames.Any())
                {
                    sb.AppendLine($"Characters: {string.Join(", ", scene.CharacterNames)}");
                }
                sb.AppendLine();
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
