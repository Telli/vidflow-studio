using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.ValueObjects;

namespace VidFlow.Api.Features.StitchPlan;

public static class GetStitchPlan
{
    public record Response(
        Guid Id,
        Guid ProjectId,
        List<StitchPlanEntryDto> Entries,
        int TotalRuntimeSeconds,
        DateTime UpdatedAt);

    public record StitchPlanEntryDto(
        Guid SceneId,
        int Order,
        string SceneNumber,
        string SceneTitle,
        int SceneRuntimeSeconds,
        string? TransitionType,
        string? TransitionNotes,
        string? AudioNotes);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId}/stitch-plan", Handler)
           .WithName("GetStitchPlan")
           .WithTags("StitchPlan");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        // Validate project exists
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return Results.NotFound($"Project with ID {projectId} not found.");

        var stitchPlan = await db.StitchPlans
            .FirstOrDefaultAsync(sp => sp.ProjectId == projectId, ct);

        if (stitchPlan == null)
        {
            // Return empty stitch plan if none exists
            var emptyResponse = new Response(
                Guid.Empty,
                projectId,
                [],
                0,
                DateTime.UtcNow);
            return Results.Ok(emptyResponse);
        }

        // Get scene details for each entry
        var sceneIds = stitchPlan.Entries.Select(e => e.SceneId).ToList();
        var scenes = await db.Scenes
            .Where(s => sceneIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => new { s.Number, s.Title, s.RuntimeTargetSeconds }, ct);

        var entries = stitchPlan.Entries
            .OrderBy(e => e.Order)
            .Select(e =>
            {
                var scene = scenes.GetValueOrDefault(e.SceneId);
                return new StitchPlanEntryDto(
                    e.SceneId,
                    e.Order,
                    scene?.Number ?? "Unknown",
                    scene?.Title ?? "Unknown Scene",
                    scene?.RuntimeTargetSeconds ?? 0,
                    e.TransitionType,
                    e.TransitionNotes,
                    e.AudioNotes);
            })
            .ToList();

        var response = new Response(
            stitchPlan.Id,
            stitchPlan.ProjectId,
            entries,
            stitchPlan.TotalRuntimeSeconds,
            stitchPlan.UpdatedAt);

        return Results.Ok(response);
    }
}
