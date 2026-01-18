using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;

namespace VidFlow.Api.Features.StoryBible;

public static class CreateStoryBible
{
    public record Request(
        string Themes,
        string WorldRules,
        string Tone,
        string VisualStyle,
        string PacingRules);

    public record Response(
        Guid Id,
        Guid ProjectId,
        int Version,
        DateTime CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId}/story-bible", Handler)
           .WithName("CreateStoryBible")
           .WithTags("StoryBible");
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

        // Check if story bible already exists for this project
        var existingStoryBible = await db.StoryBibles
            .FirstOrDefaultAsync(sb => sb.ProjectId == projectId, ct);
        if (existingStoryBible != null)
            return Results.BadRequest($"Story Bible already exists for this project.");

        // Create story bible
        var storyBible = VidFlow.Api.Domain.Entities.StoryBible.Create(
            projectId,
            request.Themes,
            request.WorldRules,
            request.Tone,
            request.VisualStyle,
            request.PacingRules);

        db.StoryBibles.Add(storyBible);

        // Append domain events to event store
        foreach (var evt in storyBible.DomainEvents)
        {
            db.EventStore.Add(EventStoreEntry.FromDomainEvent(evt, projectId, storyBible.Id));
        }
        storyBible.ClearDomainEvents();

        await db.SaveChangesAsync(ct);

        var response = new Response(
            storyBible.Id,
            storyBible.ProjectId,
            storyBible.Version,
            storyBible.CreatedAt);

        return Results.Created($"/api/story-bibles/{storyBible.Id}", response);
    }
}
