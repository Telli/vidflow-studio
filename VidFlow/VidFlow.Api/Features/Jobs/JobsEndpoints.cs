namespace VidFlow.Api.Features.Jobs;

public static class JobsEndpoints
{
    public static void MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        GetJobStatus.MapEndpoint(app);
    }
}
