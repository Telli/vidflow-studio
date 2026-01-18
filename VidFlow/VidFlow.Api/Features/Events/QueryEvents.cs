using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;

namespace VidFlow.Api.Features.Events;

public static class QueryEvents
{
    public record EventDto(
        Guid Id,
        string EventType,
        Guid? ProjectId,
        Guid? EntityId,
        string Payload,
        string EmittedBy,
        DateTime Timestamp);

    public record Response(
        List<EventDto> Events,
        int TotalCount,
        int Page,
        int PageSize);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/events", Handler)
           .WithName("QueryEvents")
           .WithTags("Events");
    }

    private static async Task<IResult> Handler(
        VidFlowDbContext db,
        Guid? projectId = null,
        Guid? entityId = null,
        string? eventType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1)
            return Results.BadRequest("page must be >= 1.");
        if (pageSize < 1 || pageSize > 200)
            return Results.BadRequest("pageSize must be between 1 and 200.");

        var query = db.EventStore.AsQueryable();

        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);

        if (entityId.HasValue)
            query = query.Where(e => e.EntityId == entityId.Value);

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(e => e.EventType.Contains(eventType));

        if (fromDate.HasValue)
            query = query.Where(e => e.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.Timestamp <= toDate.Value);

        var totalCount = await query.CountAsync(ct);

        var events = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventDto(
                e.Id,
                e.EventType,
                e.ProjectId,
                e.EntityId,
                e.Payload,
                e.EmittedBy,
                e.Timestamp))
            .ToListAsync(ct);

        return Results.Ok(new Response(events, totalCount, page, pageSize));
    }
}
