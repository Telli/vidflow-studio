using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Features.Projects;

/// <summary>
/// Feature slice for listing all projects.
/// Returns projects with computed metrics.
/// </summary>
public static class ListProjects
{
    public record ProjectSummary(
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

    public record Response(IReadOnlyList<ProjectSummary> Projects);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects", Handler)
           .WithName("ListProjects")
           .WithTags("Projects")
           .WithDescription("Lists all projects with computed metrics.")
           .Produces<Response>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handler(
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var projects = await db.Projects
            .Include(p => p.Scenes)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(ct);

        var summaries = projects.Select(p => new ProjectSummary(
            p.Id,
            p.Title,
            p.Logline,
            p.RuntimeTargetSeconds,
            p.Status.ToString(),
            p.CreatedAt,
            p.UpdatedAt,
            p.Scenes.Sum(s => s.RuntimeEstimateSeconds),
            p.Scenes.Count,
            p.Scenes.Count(s => s.Status == SceneStatus.Review)
        )).ToList();

        return Results.Ok(new Response(summaries));
    }
}
