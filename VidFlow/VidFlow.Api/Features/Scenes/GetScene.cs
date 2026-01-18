using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.Scenes.Shared;

namespace VidFlow.Api.Features.Scenes;

public static class GetScene
{
    public record Response(
        Guid Id,
        string Number,
        string Title,
        string NarrativeGoal,
        string EmotionalBeat,
        string Location,
        string TimeOfDay,
        SceneStatus Status,
        int RuntimeEstimateSeconds,
        int RuntimeTargetSeconds,
        string Script,
        int Version,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string? ApprovedBy,
        DateTime? ApprovedAt,
        bool IsLocked,
        DateTime? LockedUntil,
        string? LockedBy,
        List<string> CharacterNames,
        List<ShotDto> Shots,
        List<ProposalDto> Proposals);

    public record ShotDto(
        Guid Id,
        int Number,
        string Type,
        string Duration,
        string Description,
        string Camera);

    public record ProposalDto(
        Guid Id,
        AgentRole Role,
        string Summary,
        string Rationale,
        int RuntimeImpactSeconds,
        string Diff,
        ProposalStatus Status,
        DateTime CreatedAt,
        int TokensUsed,
        decimal CostUsd);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/scenes/{sceneId}", Handler)
           .WithName("GetScene")
           .WithTags("Scenes");
    }

    private static async Task<IResult> Handler(
        Guid sceneId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var scene = await db.Scenes
            .Include(s => s.Shots)
            .Include(s => s.Proposals)
            .FirstOrDefaultAsync(s => s.Id == sceneId, ct);

        if (scene is null)
            return Results.NotFound($"Scene with ID {sceneId} not found.");

        scene.RecalculateRuntimeEstimate();

        var response = new Response(
            scene.Id,
            scene.Number,
            scene.Title,
            scene.NarrativeGoal,
            scene.EmotionalBeat,
            scene.Location,
            scene.TimeOfDay,
            scene.Status,
            scene.RuntimeEstimateSeconds,
            scene.RuntimeTargetSeconds,
            scene.Script,
            scene.Version,
            scene.CreatedAt,
            scene.UpdatedAt,
            scene.ApprovedBy,
            scene.ApprovedAt,
            scene.IsCurrentlyLocked(),
            scene.LockedUntil,
            scene.LockedBy,
            scene.CharacterNames,
            scene.Shots.Select(s => new ShotDto(
                s.Id,
                s.Number,
                s.Type,
                s.Duration,
                s.Description,
                s.Camera)).ToList(),
            scene.Proposals.Select(p => new ProposalDto(
                p.Id,
                p.Role,
                p.Summary,
                p.Rationale,
                p.RuntimeImpactSeconds,
                p.Diff,
                p.Status,
                p.CreatedAt,
                p.TokensUsed,
                p.CostUsd)).ToList());

        return Results.Ok(response);
    }
}
