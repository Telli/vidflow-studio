using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.ValueObjects;

namespace VidFlow.Api.Features.StitchPlan;

public static class SetTransition
{
    public record Request(
        string TransitionType,
        string? TransitionNotes = null);

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
        app.MapPut("/api/projects/{projectId}/stitch-plan/scenes/{sceneId}/transition", Handler)
           .WithName("SetTransition")
           .WithTags("StitchPlan");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        Guid sceneId,
        Request request,
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
            return Results.NotFound($"Stitch plan not found for project {projectId}.");

        // Validate scene exists in stitch plan
        var entry = stitchPlan.Entries.FirstOrDefault(e => e.SceneId == sceneId);
        if (entry == null)
            return Results.NotFound($"Scene {sceneId} not found in stitch plan.");

        // Set transition
        stitchPlan.SetTransition(sceneId, request.TransitionType, request.TransitionNotes);

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

        return Results.Ok(response);
    }
}
