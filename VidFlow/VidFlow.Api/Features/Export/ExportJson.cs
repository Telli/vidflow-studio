using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;

namespace VidFlow.Api.Features.Export;

public static class ExportJson
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId}/export/json", Handler)
           .WithName("ExportProjectJson")
           .WithTags("Export");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var project = await db.Projects
            .Include(p => p.Scenes)
                .ThenInclude(s => s.Shots)
            .Include(p => p.Scenes)
                .ThenInclude(s => s.Proposals)
            .Include(p => p.Characters)
            .Include(p => p.StoryBible)
            .Include(p => p.StitchPlan)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        if (project is null)
            return Results.NotFound($"Project {projectId} not found");

        var export = new
        {
            ExportedAt = DateTime.UtcNow,
            Project = new
            {
                project.Id,
                project.Title,
                project.Logline,
                project.RuntimeTargetSeconds,
                project.Status,
                project.BudgetCapUsd,
                project.CurrentSpendUsd,
                project.CreatedAt,
                project.UpdatedAt
            },
            StoryBible = project.StoryBible != null ? new
            {
                project.StoryBible.Themes,
                project.StoryBible.WorldRules,
                project.StoryBible.Tone,
                project.StoryBible.VisualStyle,
                project.StoryBible.PacingRules,
                project.StoryBible.Version
            } : null,
            Characters = project.Characters.Select(c => new
            {
                c.Id,
                c.Name,
                c.Role,
                c.Archetype,
                c.Age,
                c.Description,
                c.Backstory,
                c.Traits,
                c.Version
            }),
            Scenes = project.Scenes.OrderBy(s => s.Number).Select(s => new
            {
                s.Id,
                s.Number,
                s.Title,
                s.NarrativeGoal,
                s.EmotionalBeat,
                s.Location,
                s.TimeOfDay,
                s.Status,
                s.Script,
                s.RuntimeTargetSeconds,
                s.RuntimeEstimateSeconds,
                s.Version,
                s.CharacterNames,
                Shots = s.Shots.OrderBy(sh => sh.Number).Select(sh => new
                {
                    sh.Number,
                    sh.Type,
                    sh.Duration,
                    sh.Description,
                    sh.Camera
                }),
                Proposals = s.Proposals.Select(p => new
                {
                    p.Role,
                    p.Summary,
                    p.Status,
                    p.RuntimeImpactSeconds
                })
            }),
            StitchPlan = project.StitchPlan != null ? new
            {
                project.StitchPlan.TotalRuntimeSeconds,
                Entries = project.StitchPlan.Entries.Select(e => new
                {
                    e.SceneId,
                    e.Order,
                    e.TransitionType,
                    e.TransitionNotes,
                    e.AudioNotes
                })
            } : null
        };

        return Results.Ok(export);
    }
}
