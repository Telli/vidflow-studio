using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;

namespace VidFlow.Api.Features.StoryBible;

public static class UpdateStoryBible
{
    public record Request(
        string? Themes = null,
        string? WorldRules = null,
        string? Tone = null,
        string? VisualStyle = null,
        string? PacingRules = null);

    public record Response(
        Guid Id,
        Guid ProjectId,
        int Version,
        DateTime CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/projects/{projectId}/story-bible", Handler)
           .WithName("UpdateStoryBible")
           .WithTags("StoryBible");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        Request request,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var storyBible = await db.StoryBibles
            .FirstOrDefaultAsync(sb => sb.ProjectId == projectId, ct);
        
        if (storyBible is null)
            return Results.NotFound($"Story Bible not found for project {projectId}.");

        // Update story bible (creates new version)
        storyBible.Update(
            request.Themes,
            request.WorldRules,
            request.Tone,
            request.VisualStyle,
            request.PacingRules);

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

        return Results.Ok(response);
    }
}
