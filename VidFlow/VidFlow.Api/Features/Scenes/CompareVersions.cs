using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;

namespace VidFlow.Api.Features.Scenes;

public static class CompareVersions
{
    public record VersionInfo(
        int Version,
        DateTime UpdatedAt,
        string Script,
        string NarrativeGoal,
        string EmotionalBeat,
        int ShotCount);

    public record Response(
        Guid SceneId,
        VersionInfo CurrentVersion,
        List<EventChange> ChangeHistory);

    public record EventChange(
        string EventType,
        int Version,
        DateTime Timestamp,
        string EmittedBy,
        string Summary);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/scenes/{sceneId}/versions", Handler)
           .WithName("GetSceneVersionHistory")
           .WithTags("Scenes");
    }

    private static async Task<IResult> Handler(
        Guid sceneId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var scene = await db.Scenes
            .Include(s => s.Shots)
            .FirstOrDefaultAsync(s => s.Id == sceneId, ct);

        if (scene is null)
            return Results.NotFound($"Scene {sceneId} not found");

        // Get all events for this scene to build history
        var events = await db.EventStore
            .Where(e => e.EntityId == sceneId)
            .OrderByDescending(e => e.Timestamp)
            .Take(50)
            .ToListAsync(ct);

        var currentVersion = new VersionInfo(
            scene.Version,
            scene.UpdatedAt,
            scene.Script,
            scene.NarrativeGoal,
            scene.EmotionalBeat,
            scene.Shots.Count);

        var changeHistory = events.Select(e => new EventChange(
            e.EventType,
            ExtractVersion(e.Payload),
            e.Timestamp,
            e.EmittedBy,
            GetEventSummary(e.EventType))).ToList();

        return Results.Ok(new Response(sceneId, currentVersion, changeHistory));
    }

    private static int ExtractVersion(string payload)
    {
        try
        {
            if (payload.Contains("\"NewVersion\""))
            {
                var start = payload.IndexOf("\"NewVersion\":") + 13;
                var end = payload.IndexOf(',', start);
                if (end == -1) end = payload.IndexOf('}', start);
                return int.Parse(payload[start..end].Trim());
            }
            if (payload.Contains("\"Version\""))
            {
                var start = payload.IndexOf("\"Version\":") + 10;
                var end = payload.IndexOf(',', start);
                if (end == -1) end = payload.IndexOf('}', start);
                return int.Parse(payload[start..end].Trim());
            }
        }
        catch { }
        return 0;
    }

    private static string GetEventSummary(string eventType) => eventType switch
    {
        "SceneCreated" => "Scene was created",
        "SceneUpdated" => "Scene content was updated",
        "SceneSubmittedForReview" => "Scene submitted for review",
        "SceneApproved" => "Scene was approved",
        "SceneRevisionRequested" => "Revision was requested",
        "AgentProposalCreated" => "Agent created a proposal",
        _ => $"Event: {eventType}"
    };
}
