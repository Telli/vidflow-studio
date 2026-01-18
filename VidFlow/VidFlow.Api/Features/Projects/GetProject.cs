using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Features.Projects;

/// <summary>
/// Feature slice for retrieving a project with computed metrics.
/// Returns total runtime, scene count, and pending reviews.
/// </summary>
public static class GetProject
{
    public record Response(
        Guid Id,
        string Title,
        string Logline,
        int RuntimeTargetSeconds,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int TotalRuntimeSeconds,
        int SceneCount,
        int PendingReviewCount);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}", Handler)
           .WithName("GetProject")
           .WithTags("Projects")
           .WithDescription("Retrieves a project with computed metrics including total runtime, scene count, and pending reviews.")
           .Produces<Response>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var project = await db.Projects
            .Include(p => p.Scenes)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        if (project is null)
            return Results.NotFound();

        // Compute metrics
        var totalRuntime = project.Scenes.Sum(s => s.RuntimeEstimateSeconds);
        var sceneCount = project.Scenes.Count;
        var pendingReviewCount = project.Scenes.Count(s => s.Status == SceneStatus.Review);

        var response = new Response(
            project.Id,
            project.Title,
            project.Logline,
            project.RuntimeTargetSeconds,
            project.Status.ToString(),
            project.CreatedAt,
            project.UpdatedAt,
            totalRuntime,
            sceneCount,
            pendingReviewCount);

        return Results.Ok(response);
    }
}
