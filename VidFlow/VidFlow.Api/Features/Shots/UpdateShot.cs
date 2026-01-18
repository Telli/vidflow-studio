using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.Exceptions;

namespace VidFlow.Api.Features.Shots;

public static class UpdateShot
{
    public record Request(
        string? Type = null,
        string? Duration = null,
        string? Description = null,
        string? Camera = null);

    public record Response(
        Guid Id,
        Guid SceneId,
        int Number,
        string Type,
        string Duration,
        string Description,
        string Camera);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/shots/{shotId}", Handler)
           .WithName("UpdateShot")
           .WithTags("Shots");
    }

    private static async Task<IResult> Handler(
        Guid shotId,
        Request request,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var shot = await db.Shots.FindAsync([shotId], ct);
        if (shot is null)
            return Results.NotFound($"Shot with ID {shotId} not found.");

        // Get scene (with shots) to update version and runtime estimate
        var scene = await db.Scenes
            .Include(s => s.Shots)
            .FirstOrDefaultAsync(s => s.Id == shot.SceneId, ct);

        if (scene is null)
            return Results.NotFound($"Scene with ID {shot.SceneId} not found.");

        if (scene.IsCurrentlyLocked())
            throw new ConcurrentModificationException(scene.Id);

        // Update shot
        shot.Update(
            request.Type,
            request.Duration,
            request.Description,
            request.Camera);

        scene.RecalculateRuntimeEstimate();

        // Update scene version when shot is modified
        scene.Update();
        foreach (var evt in scene.DomainEvents)
        {
            db.EventStore.Add(EventStoreEntry.FromDomainEvent(evt, scene.ProjectId, scene.Id));
        }
        scene.ClearDomainEvents();

        await db.SaveChangesAsync(ct);

        var response = new Response(
            shot.Id,
            shot.SceneId,
            shot.Number,
            shot.Type,
            shot.Duration,
            shot.Description,
            shot.Camera);

        return Results.Ok(response);
    }
}
