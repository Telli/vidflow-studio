using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;

namespace VidFlow.Api.Features.Assets;

public static class ListAssets
{
    public record AssetDto(
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

    public record Response(List<AssetDto> Assets);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId}/assets", Handler)
           .WithName("ListAssets")
           .WithTags("Assets");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        VidFlowDbContext db,
        Guid? sceneId,
        Guid? shotId,
        CancellationToken ct)
    {
        var query = db.Assets.AsQueryable().Where(a => a.ProjectId == projectId);

        if (sceneId.HasValue)
            query = query.Where(a => a.SceneId == sceneId.Value);

        if (shotId.HasValue)
            query = query.Where(a => a.ShotId == shotId.Value);

        var assets = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AssetDto(
                a.Id,
                a.ProjectId,
                a.SceneId,
                a.ShotId,
                a.Type.ToString(),
                a.Status.ToString(),
                a.Name,
                a.Prompt,
                a.Provider,
                a.Url,
                a.ErrorMessage,
                a.CreatedAt,
                a.CompletedAt))
            .ToListAsync(ct);

        return Results.Ok(new Response(assets));
    }
}
