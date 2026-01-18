using VidFlow.Api.Features.Shots;

namespace VidFlow.Api.Features.Shots;

public static class ShotEndpoints
{
    public static void MapShotEndpoints(this IEndpointRouteBuilder app)
    {
        AddShot.MapEndpoint(app);
        UpdateShot.MapEndpoint(app);
        ReorderShots.MapEndpoint(app);
    }
}
