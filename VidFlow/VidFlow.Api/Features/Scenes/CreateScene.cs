using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.Scenes.Shared;

namespace VidFlow.Api.Features.Scenes;

public static class CreateScene
{
    public record Request(
        string Number,
        string Title,
        string NarrativeGoal,
        string EmotionalBeat,
        string Location,
        string TimeOfDay,
        int RuntimeTargetSeconds,
        List<string>? CharacterNames = null);

    public record Response(
        Guid Id,
        string Number,
        string Title,
        SceneStatus Status,
        int Version,
        DateTime CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId}/scenes", Handler)
           .WithName("CreateScene")
           .WithTags("Scenes");
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

        // Create scene
        var scene = Scene.Create(
            projectId,
            request.Number,
            request.Title,
            request.NarrativeGoal,
            request.EmotionalBeat,
            request.Location,
            request.TimeOfDay,
            request.RuntimeTargetSeconds,
            request.CharacterNames);

        db.Scenes.Add(scene);

        // Append domain events to event store
        foreach (var evt in scene.DomainEvents)
        {
            db.EventStore.Add(EventStoreEntry.FromDomainEvent(evt, projectId, scene.Id));
        }
        scene.ClearDomainEvents();

        await db.SaveChangesAsync(ct);

        var response = new Response(
            scene.Id,
            scene.Number,
            scene.Title,
            scene.Status,
            scene.Version,
            scene.CreatedAt);

        return Results.Created($"/api/scenes/{scene.Id}", response);
    }
}
