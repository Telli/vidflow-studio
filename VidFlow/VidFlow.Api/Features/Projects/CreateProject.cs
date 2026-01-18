using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;

namespace VidFlow.Api.Features.Projects;

/// <summary>
/// Feature slice for creating a new project.
/// Validates runtime target bounds and emits ProjectCreated event.
/// </summary>
public static class CreateProject
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
        DateTime CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects", Handler)
           .WithName("CreateProject")
           .WithTags("Projects")
           .WithDescription("Creates a new film project with title, logline, and runtime target (300-1200 seconds).")
           .Produces<Response>(StatusCodes.Status201Created)
           .ProducesValidationProblem()
           .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Handler(
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

        // Create project (this also adds ProjectCreated domain event)
        var project = Project.Create(
            request.Title,
            request.Logline,
            request.RuntimeTargetSeconds);

        db.Projects.Add(project);

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
            project.CreatedAt);

        return Results.Created($"/api/projects/{project.Id}", response);
    }
}
