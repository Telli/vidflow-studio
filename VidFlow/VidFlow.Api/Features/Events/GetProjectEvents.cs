using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;

namespace VidFlow.Api.Features.Events;

public static class GetProjectEvents
{
    public record EventDto(
        Guid Id,
        string EventType,
        Guid? EntityId,
        string Payload,
        string EmittedBy,
        DateTime Timestamp);

    public record Response(
        Guid ProjectId,
        List<EventDto> Events,
        int TotalCount);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId}/events", Handler)
           .WithName("GetProjectEvents")
           .WithTags("Events");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        VidFlowDbContext db,
        int limit = 100,
        CancellationToken ct = default)
    {
        if (limit < 1 || limit > 500)
            return Results.BadRequest("limit must be between 1 and 500.");

        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return Results.NotFound($"Project {projectId} not found");

        var events = await db.EventStore
            .Where(e => e.ProjectId == projectId)
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .Select(e => new EventDto(
                e.Id,
                e.EventType,
                e.EntityId,
                e.Payload,
                e.EmittedBy,
                e.Timestamp))
            .ToListAsync(ct);

        var totalCount = await db.EventStore
            .Where(e => e.ProjectId == projectId)
            .CountAsync(ct);

        return Results.Ok(new Response(projectId, events, totalCount));
    }
}
