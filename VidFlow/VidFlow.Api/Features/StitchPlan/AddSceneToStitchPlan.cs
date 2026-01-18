using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.ValueObjects;

namespace VidFlow.Api.Features.StitchPlan;

public static class AddSceneToStitchPlan
{
    public record Request(
        Guid SceneId,
        int Order);

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
        app.MapPost("/api/projects/{projectId}/stitch-plan/scenes", Handler)
           .WithName("AddSceneToStitchPlan")
           .WithTags("StitchPlan");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        Request request,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        // Validate project exists
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return Results.NotFound($"Project with ID {projectId} not found.");

        // Validate scene exists and belongs to project
        var scene = await db.Scenes
            .FirstOrDefaultAsync(s => s.Id == request.SceneId && s.ProjectId == projectId, ct);
        if (scene is null)
            return Results.NotFound($"Scene with ID {request.SceneId} not found in project {projectId}.");

        // Validate scene is approved
        if (scene.Status != SceneStatus.Approved)
            return Results.BadRequest($"Scene {request.SceneId} must be approved before adding to stitch plan.");

        // Get or create stitch plan
        var stitchPlan = await db.StitchPlans
            .FirstOrDefaultAsync(sp => sp.ProjectId == projectId, ct);

        if (stitchPlan == null)
        {
            stitchPlan = VidFlow.Api.Domain.Entities.StitchPlan.Create(projectId);
            db.StitchPlans.Add(stitchPlan);
        }

        // Check if scene already exists in stitch plan
        if (stitchPlan.Entries.Any(e => e.SceneId == request.SceneId))
            return Results.BadRequest($"Scene {request.SceneId} already exists in stitch plan.");

        // Validate order is reasonable
        if (request.Order < 1 || request.Order > stitchPlan.Entries.Count + 1)
            return Results.BadRequest($"Invalid order {request.Order}. Must be between 1 and {stitchPlan.Entries.Count + 1}.");

        // Create new entry
        var entry = new StitchPlanEntry(
            request.SceneId,
            request.Order,
            null, // TransitionType
            null, // TransitionNotes
            null  // AudioNotes
        );

        // Add entry and reorder existing entries if needed
        stitchPlan.AddEntry(entry);
        stitchPlan.ReorderEntries();

        // Update total runtime
        var entrySceneIds = stitchPlan.Entries.Select(e => e.SceneId).ToList();
        var entrySceneRuntimes = await db.Scenes
            .Where(s => s.ProjectId == projectId && entrySceneIds.Contains(s.Id))
            .Select(s => new { s.Id, s.RuntimeTargetSeconds })
            .ToDictionaryAsync(x => x.Id, x => x.RuntimeTargetSeconds, ct);

        var totalRuntime = stitchPlan.Entries.Sum(e => entrySceneRuntimes.GetValueOrDefault(e.SceneId));
        stitchPlan.SetTotalRuntime(totalRuntime);

        await db.SaveChangesAsync(ct);

        // Get scene details for response
        var sceneIds = stitchPlan.Entries.Select(e => e.SceneId).ToList();
        var scenes = await db.Scenes
            .Where(s => sceneIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => new { s.Number, s.Title, s.RuntimeTargetSeconds }, ct);

        var entries = stitchPlan.Entries
            .OrderBy(e => e.Order)
            .Select(e =>
            {
                var sceneData = scenes.GetValueOrDefault(e.SceneId);
                return new StitchPlanEntryDto(
                    e.SceneId,
                    e.Order,
                    sceneData?.Number ?? "Unknown",
                    sceneData?.Title ?? "Unknown Scene",
                    sceneData?.RuntimeTargetSeconds ?? 0,
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

        return Results.Created($"/api/projects/{projectId}/stitch-plan", response);
    }
}
