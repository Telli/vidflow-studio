using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Features.Render;

public static class GetRenderJob
{
    public record Response(
        Guid Id,
        Guid ProjectId,
        Guid? SceneId,
        RenderType Type,
        RenderStatus Status,
        int ProgressPercent,
        string? ArtifactPath,
        int? SourceVersion,
        string? ErrorMessage,
        DateTime CreatedAt,
        DateTime? StartedAt,
        DateTime? CompletedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/render-jobs/{jobId}", Handler)
           .WithName("GetRenderJob")
           .WithTags("Render");
    }

    private static async Task<IResult> Handler(
        Guid jobId,
        RenderService renderService,
        CancellationToken ct)
    {
        var job = await renderService.GetRenderJobAsync(jobId, ct);
        if (job is null)
            return Results.NotFound($"Render job {jobId} not found");

        var response = new Response(
            job.Id,
            job.ProjectId,
            job.SceneId,
            job.Type,
            job.Status,
            job.ProgressPercent,
            job.ArtifactPath,
            job.SourceVersion,
            job.ErrorMessage,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt);

        return Results.Ok(response);
    }
}
