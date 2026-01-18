using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Features.Render;

public static class RequestFinalRender
{
    public record Response(
        Guid JobId,
        Guid ProjectId,
        RenderType Type,
        RenderStatus Status,
        DateTime CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId}/render-final", Handler)
           .WithName("RequestFinalRender")
           .WithTags("Render");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        RenderService renderService,
        CancellationToken ct)
    {
        try
        {
            var job = await renderService.RequestFinalRenderAsync(projectId, ct);

            var response = new Response(
                job.Id,
                projectId,
                job.Type,
                job.Status,
                job.CreatedAt);

            return Results.Accepted($"/api/render-jobs/{job.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}
