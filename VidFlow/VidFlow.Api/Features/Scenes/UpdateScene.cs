using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.Scenes.Shared;

namespace VidFlow.Api.Features.Scenes;

public static class UpdateScene
{
    public record Request(
        string? Title = null,
        string? NarrativeGoal = null,
        string? EmotionalBeat = null,
        string? Location = null,
        string? TimeOfDay = null,
        string? Script = null,
        List<string>? CharacterNames = null);

    public record Response(
        Guid Id,
        string Number,
        string Title,
        SceneStatus Status,
        int Version,
        DateTime UpdatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/scenes/{sceneId}", Handler)
           .WithName("UpdateScene")
           .WithTags("Scenes");
    }

    private static async Task<IResult> Handler(
        Guid sceneId,
        Request request,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var scene = await db.Scenes.FindAsync([sceneId], ct);
        if (scene is null)
            return Results.NotFound($"Scene with ID {sceneId} not found.");

        // Update scene
        scene.Update(
            request.Title,
            request.NarrativeGoal,
            request.EmotionalBeat,
            request.Location,
            request.TimeOfDay,
            request.Script,
            request.CharacterNames);

        // Append domain events to event store
        foreach (var evt in scene.DomainEvents)
        {
            db.EventStore.Add(EventStoreEntry.FromDomainEvent(evt, scene.ProjectId, scene.Id));
        }
        scene.ClearDomainEvents();

        await db.SaveChangesAsync(ct);

        var response = new Response(
            scene.Id,
            scene.Number,
            scene.Title,
            scene.Status,
            scene.Version,
            scene.UpdatedAt);

        return Results.Ok(response);
    }
}
