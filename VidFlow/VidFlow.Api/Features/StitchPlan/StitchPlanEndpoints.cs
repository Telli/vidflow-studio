using VidFlow.Api.Features.StitchPlan;

namespace VidFlow.Api.Features.StitchPlan;

public static class StitchPlanEndpoints
{
    public static void MapStitchPlanEndpoints(this IEndpointRouteBuilder app)
    {
        GetStitchPlan.MapEndpoint(app);
        AddSceneToStitchPlan.MapEndpoint(app);
        SetTransition.MapEndpoint(app);
        SetAudioNote.MapEndpoint(app);
    }
}
