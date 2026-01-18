using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Features.Render;

public static class ListRenderJobs
{
    public record RenderJobDto(
        Guid Id,
        Guid? SceneId,
        RenderType Type,
        RenderStatus Status,
        int ProgressPercent,
        string? ArtifactPath,
        DateTime CreatedAt,
        DateTime? CompletedAt);

    public record Response(
        Guid ProjectId,
        List<RenderJobDto> Jobs);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId}/render-jobs", Handler)
           .WithName("ListRenderJobs")
           .WithTags("Render");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        RenderService renderService,
        CancellationToken ct)
    {
        var jobs = await renderService.GetRenderJobsForProjectAsync(projectId, ct);

        var jobDtos = jobs.Select(j => new RenderJobDto(
            j.Id,
            j.SceneId,
            j.Type,
            j.Status,
            j.ProgressPercent,
            j.ArtifactPath,
            j.CreatedAt,
            j.CompletedAt)).ToList();

        return Results.Ok(new Response(projectId, jobDtos));
    }
}
