using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.Exceptions;

namespace VidFlow.Api.Features.Shots;

public static class AddShot
{
    public record Request(
        string Type,
        string Duration,
        string Description,
        string Camera);

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
        app.MapPost("/api/scenes/{sceneId}/shots", Handler)
           .WithName("AddShot")
           .WithTags("Shots");
    }

    private static async Task<IResult> Handler(
        Guid sceneId,
        Request request,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var scene = await db.Scenes
            .Include(s => s.Shots)
            .FirstOrDefaultAsync(s => s.Id == sceneId, ct);
        
        if (scene is null)
            return Results.NotFound($"Scene with ID {sceneId} not found.");

        if (scene.IsCurrentlyLocked())
            throw new ConcurrentModificationException(scene.Id);

        // Get next shot number
        var nextNumber = scene.Shots.Any() ? scene.Shots.Max(s => s.Number) + 1 : 1;

        // Create shot
        var shot = Shot.Create(
            sceneId,
            nextNumber,
            request.Type,
            request.Duration,
            request.Description,
            request.Camera);

        db.Shots.Add(shot);
        scene.Shots.Add(shot);
        scene.RecalculateRuntimeEstimate();

        // Update scene version when shot is added
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

        return Results.Created($"/api/shots/{shot.Id}", response);
    }
}
