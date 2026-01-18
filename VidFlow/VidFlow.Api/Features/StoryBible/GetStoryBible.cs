using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;

namespace VidFlow.Api.Features.StoryBible;

public static class GetStoryBible
{
    public record Response(
        Guid Id,
        Guid ProjectId,
        string Themes,
        string WorldRules,
        string Tone,
        string VisualStyle,
        string PacingRules,
        int Version,
        DateTime CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId}/story-bible", Handler)
           .WithName("GetStoryBible")
           .WithTags("StoryBible");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        // Validate project exists
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return Results.NotFound($"Project with ID {projectId} not found.");

        var storyBible = await db.StoryBibles
            .FirstOrDefaultAsync(sb => sb.ProjectId == projectId, ct);

        if (storyBible is null)
            return Results.NotFound($"Story Bible not found for project {projectId}.");

        var response = new Response(
            storyBible.Id,
            storyBible.ProjectId,
            storyBible.Themes,
            storyBible.WorldRules,
            storyBible.Tone,
            storyBible.VisualStyle,
            storyBible.PacingRules,
            storyBible.Version,
            storyBible.CreatedAt);

        return Results.Ok(response);
    }
}
