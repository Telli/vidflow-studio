using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.Scenes.Shared;

namespace VidFlow.Api.Features.Scenes;

public static class RequestRevision
{
    public record Request(
        string Feedback,
        string RequestedBy);

    public record Response(
        Guid Id,
        string Number,
        string Title,
        SceneStatus Status,
        DateTime UpdatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/scenes/{sceneId}/request-revision", Handler)
           .WithName("RequestSceneRevision")
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

        // Request revision
        scene.RequestRevision(request.Feedback, request.RequestedBy);

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
            scene.UpdatedAt);

        return Results.Ok(response);
    }
}
