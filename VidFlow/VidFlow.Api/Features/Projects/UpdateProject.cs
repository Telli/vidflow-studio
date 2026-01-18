using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;

namespace VidFlow.Api.Features.Projects;

/// <summary>
/// Feature slice for updating project metadata.
/// Validates runtime target bounds and emits ProjectUpdated event.
/// </summary>
public static class UpdateProject
{
    public record Request(
        string Title,
        string Logline,
        int RuntimeTargetSeconds);

    public record Response(
        Guid Id,
        string Title,
        string Logline,
        int RuntimeTargetSeconds,
        string Status,
        DateTime UpdatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/projects/{projectId:guid}", Handler)
           .WithName("UpdateProject")
           .WithTags("Projects")
           .WithDescription("Updates project metadata. Runtime target must be between 300 and 1200 seconds.")
           .Produces<Response>(StatusCodes.Status200OK)
           .ProducesValidationProblem()
           .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        Request request,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Title", ["Title is required."] }
            });

        if (string.IsNullOrWhiteSpace(request.Logline))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Logline", ["Logline is required."] }
            });

        if (request.RuntimeTargetSeconds < 300 || request.RuntimeTargetSeconds > 1200)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "RuntimeTargetSeconds", ["Runtime target must be between 300 and 1200 seconds."] }
            });

        var project = await db.Projects.FindAsync([projectId], ct);

        if (project is null)
            return Results.NotFound();

        // Update project (this also adds ProjectUpdated domain event)
        project.Update(
            request.Title,
            request.Logline,
            request.RuntimeTargetSeconds);

        // Append domain events to event store
        foreach (var evt in project.DomainEvents)
        {
            db.EventStore.Add(EventStoreEntry.FromDomainEvent(evt, project.Id, project.Id));
        }
        project.ClearDomainEvents();

        await db.SaveChangesAsync(ct);

        var response = new Response(
            project.Id,
            project.Title,
            project.Logline,
            project.RuntimeTargetSeconds,
            project.Status.ToString(),
            project.UpdatedAt);

        return Results.Ok(response);
    }
}
