using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.Exceptions;

namespace VidFlow.Api.Features.Shots;

public static class ReorderShots
{
    public record Request(
        List<ShotOrderDto> ShotOrders);

    public record ShotOrderDto(
        Guid ShotId,
        int NewNumber);

    public record Response(
        List<ShotDto> Shots);

    public record ShotDto(
        Guid Id,
        Guid SceneId,
        int Number,
        string Type,
        string Duration,
        string Description,
        string Camera);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/scenes/{sceneId}/shots/reorder", Handler)
           .WithName("ReorderShots")
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

        // Validate all shot IDs belong to this scene
        var sceneShotIds = scene.Shots.Select(s => s.Id).ToHashSet();
        var invalidShotIds = request.ShotOrders
            .Where(so => !sceneShotIds.Contains(so.ShotId))
            .Select(so => so.ShotId)
            .ToList();

        if (invalidShotIds.Any())
            return Results.BadRequest($"Shot IDs {string.Join(", ", invalidShotIds)} do not belong to scene {sceneId}.");

        // Validate sequential numbering starting from 1
        var expectedNumbers = Enumerable.Range(1, request.ShotOrders.Count).ToHashSet();
        var providedNumbers = request.ShotOrders.Select(so => so.NewNumber).ToHashSet();
        
        if (!expectedNumbers.SetEquals(providedNumbers))
            return Results.BadRequest($"Shot numbers must be sequential starting from 1. Expected: {string.Join(", ", expectedNumbers)}");

        // Update shot numbers
        foreach (var shotOrder in request.ShotOrders)
        {
            var shot = scene.Shots.First(s => s.Id == shotOrder.ShotId);
            shot.SetNumber(shotOrder.NewNumber);
        }

        scene.RecalculateRuntimeEstimate();

        // Update scene version when shots are reordered
        scene.Update();
        foreach (var evt in scene.DomainEvents)
        {
            db.EventStore.Add(EventStoreEntry.FromDomainEvent(evt, scene.ProjectId, scene.Id));
        }
        scene.ClearDomainEvents();

        await db.SaveChangesAsync(ct);

        // Return updated shots in order
        var updatedShots = scene.Shots
            .OrderBy(s => s.Number)
            .Select(s => new ShotDto(
                s.Id,
                s.SceneId,
                s.Number,
                s.Type,
                s.Duration,
                s.Description,
                s.Camera))
            .ToList();

        var response = new Response(updatedShots);
        return Results.Ok(response);
    }
}
