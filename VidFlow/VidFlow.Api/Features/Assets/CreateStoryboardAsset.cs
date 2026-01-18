using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;

namespace VidFlow.Api.Features.Assets;

public static class CreateStoryboardAsset
{
    public record Request(string Name, string? Prompt);

    public record Response(
        Guid Id,
        Guid ProjectId,
        Guid SceneId,
        Guid ShotId,
        string Type,
        string Status,
        string Name,
        string? Prompt,
        string? Provider,
        string? Url,
        DateTime CreatedAt,
        DateTime? CompletedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/scenes/{sceneId}/shots/{shotId}/assets/storyboard", Handler)
           .WithName("CreateStoryboardAsset")
           .WithTags("Assets");
    }

    private static async Task<IResult> Handler(
        Guid sceneId,
        Guid shotId,
        Request request,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var scene = await db.Scenes
            .Include(s => s.Shots)
            .FirstOrDefaultAsync(s => s.Id == sceneId, ct);

        if (scene is null)
            return Results.NotFound($"Scene {sceneId} not found");

        var shot = scene.Shots.FirstOrDefault(s => s.Id == shotId);
        if (shot is null)
            return Results.NotFound($"Shot {shotId} not found in scene {sceneId}");

        var safeText = Uri.EscapeDataString($"{scene.Title} - Shot {shot.Number}");
        var placeholderUrl = $"https://placehold.co/1280x720/png?text={safeText}";

        var asset = Asset.CreateStoryboardPlaceholder(
            scene.ProjectId,
            scene.Id,
            shot.Id,
            request.Name,
            request.Prompt,
            placeholderUrl);

        db.Assets.Add(asset);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new Response(
            asset.Id,
            asset.ProjectId,
            asset.SceneId!.Value,
            asset.ShotId!.Value,
            asset.Type.ToString(),
            asset.Status.ToString(),
            asset.Name,
            asset.Prompt,
            asset.Provider,
            asset.Url,
            asset.CreatedAt,
            asset.CompletedAt));
    }
}
