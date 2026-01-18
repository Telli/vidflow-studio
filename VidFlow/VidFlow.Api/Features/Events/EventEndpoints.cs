namespace VidFlow.Api.Features.Events;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        QueryEvents.MapEndpoint(app);
        GetProjectEvents.MapEndpoint(app);
    }
}
