namespace VidFlow.Api.Features.Assets;

public static class AssetEndpoints
{
    public static void MapAssetEndpoints(this IEndpointRouteBuilder app)
    {
        CreateStoryboardAsset.MapEndpoint(app);
        ListAssets.MapEndpoint(app);
        GetAsset.MapEndpoint(app);
    }
}
