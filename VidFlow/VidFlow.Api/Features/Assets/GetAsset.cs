using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;

namespace VidFlow.Api.Features.Assets;

public static class GetAsset
{
    public record Response(
        Guid Id,
        Guid ProjectId,
        Guid? SceneId,
        Guid? ShotId,
        string Type,
        string Status,
        string Name,
        string? Prompt,
        string? Provider,
        string? Url,
        string? ErrorMessage,
        DateTime CreatedAt,
        DateTime? CompletedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/assets/{assetId}", Handler)
           .WithName("GetAsset")
           .WithTags("Assets");
    }

    private static async Task<IResult> Handler(
        Guid assetId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset is null)
            return Results.NotFound($"Asset {assetId} not found");

        return Results.Ok(new Response(
            asset.Id,
            asset.ProjectId,
            asset.SceneId,
            asset.ShotId,
            asset.Type.ToString(),
            asset.Status.ToString(),
            asset.Name,
            asset.Prompt,
            asset.Provider,
            asset.Url,
            asset.ErrorMessage,
            asset.CreatedAt,
            asset.CompletedAt));
    }
}
