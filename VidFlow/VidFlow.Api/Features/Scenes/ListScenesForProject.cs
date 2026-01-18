using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Features.Scenes;

public static class ListScenesForProject
{
    public record Response(IReadOnlyList<SceneDto> Scenes);

    public record SceneDto(
        Guid Id,
        Guid ProjectId,
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
        app.MapGet("/api/projects/{projectId}/scenes", Handler)
           .WithName("ListScenesForProject")
           .WithTags("Scenes");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Id == projectId, ct);
        if (!projectExists)
            return Results.NotFound($"Project with ID {projectId} not found.");

        var scenes = await db.Scenes
            .Where(s => s.ProjectId == projectId)
            .Include(s => s.Shots)
            .Include(s => s.Proposals)
            .OrderBy(s => s.Number)
            .ToListAsync(ct);

        var responseScenes = scenes.Select(scene =>
        {
            var runtimeEstimateSeconds = scene.Shots.Sum(s => s.GetDurationSeconds());

            return new SceneDto(
                scene.Id,
                scene.ProjectId,
                scene.Number,
                scene.Title,
                scene.NarrativeGoal,
                scene.EmotionalBeat,
                scene.Location,
                scene.TimeOfDay,
                scene.Status,
                runtimeEstimateSeconds,
                scene.RuntimeTargetSeconds,
                scene.Script,
                scene.Version,
                scene.CreatedAt,
                scene.UpdatedAt,
                scene.IsCurrentlyLocked(),
                scene.LockedUntil,
                scene.LockedBy,
                scene.CharacterNames,
                scene.Shots.OrderBy(s => s.Number).Select(s => new ShotDto(
                    s.Id,
                    s.Number,
                    s.Type,
                    s.Duration,
                    s.Description,
                    s.Camera)).ToList(),
                scene.Proposals.OrderByDescending(p => p.CreatedAt).Select(p => new ProposalDto(
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
        }).ToList();

        return Results.Ok(new Response(responseScenes));
    }
}
