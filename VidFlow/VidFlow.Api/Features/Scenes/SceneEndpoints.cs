using VidFlow.Api.Features.Scenes;

namespace VidFlow.Api.Features.Scenes;

public static class SceneEndpoints
{
    public static void MapSceneEndpoints(this IEndpointRouteBuilder app)
    {
        CreateScene.MapEndpoint(app);
        ListScenesForProject.MapEndpoint(app);
        GetScene.MapEndpoint(app);
        UpdateScene.MapEndpoint(app);
        SubmitForReview.MapEndpoint(app);
        ApproveScene.MapEndpoint(app);
        RequestRevision.MapEndpoint(app);
        GenerateScript.MapEndpoint(app);
        CompareVersions.MapEndpoint(app);
    }
}
