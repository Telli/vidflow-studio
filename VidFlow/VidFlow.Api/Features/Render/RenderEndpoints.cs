namespace VidFlow.Api.Features.Render;

public static class RenderEndpoints
{
    public static void MapRenderEndpoints(this IEndpointRouteBuilder app)
    {
        RequestAnimatic.MapEndpoint(app);
        RequestSceneRender.MapEndpoint(app);
        RequestFinalRender.MapEndpoint(app);
        GetRenderJob.MapEndpoint(app);
        ListRenderJobs.MapEndpoint(app);
    }
}
