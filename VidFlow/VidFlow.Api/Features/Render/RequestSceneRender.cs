using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Features.Render;

public static class RequestSceneRender
{
    public record Response(
        Guid JobId,
        Guid SceneId,
        RenderType Type,
        RenderStatus Status,
        DateTime CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/scenes/{sceneId}/render", Handler)
           .WithName("RequestSceneRender")
           .WithTags("Render");
    }

    private static async Task<IResult> Handler(
        Guid sceneId,
        RenderService renderService,
        CancellationToken ct)
    {
        try
        {
            var job = await renderService.RequestSceneRenderAsync(sceneId, ct);

            var response = new Response(
                job.Id,
                sceneId,
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
