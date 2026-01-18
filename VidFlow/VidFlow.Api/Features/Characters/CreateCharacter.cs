using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.ValueObjects;

namespace VidFlow.Api.Features.Characters;

public static class CreateCharacter
{
    public record Request(
        string Name,
        string Role,
        string Archetype,
        string Age,
        string Description,
        string Backstory,
        List<string>? Traits = null,
        List<CharacterRelationshipDto>? Relationships = null);

    public record CharacterRelationshipDto(
        string Name,
        string Type,
        string Note);

    public record Response(
        Guid Id,
        string Name,
        string Role,
        int Version,
        DateTime CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId}/characters", Handler)
           .WithName("CreateCharacter")
           .WithTags("Characters");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        Request request,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        // Validate project exists
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return Results.NotFound($"Project with ID {projectId} not found.");

        // Check for duplicate character name within project
        var existingCharacter = await db.Characters
            .FirstOrDefaultAsync(c => c.ProjectId == projectId && c.Name == request.Name, ct);
        if (existingCharacter != null)
            return Results.BadRequest($"Character with name '{request.Name}' already exists in this project.");

        // Convert relationships
        var relationships = request.Relationships?.Select(r => 
            new CharacterRelationship(r.Name, r.Type, r.Note));

        // Create character
        var character = Character.Create(
            projectId,
            request.Name,
            request.Role,
            request.Archetype,
            request.Age,
            request.Description,
            request.Backstory,
            request.Traits,
            relationships);

        db.Characters.Add(character);

        // Append domain events to event store
        foreach (var evt in character.DomainEvents)
        {
            db.EventStore.Add(EventStoreEntry.FromDomainEvent(evt, projectId, character.Id));
        }
        character.ClearDomainEvents();

        await db.SaveChangesAsync(ct);

        var response = new Response(
            character.Id,
            character.Name,
            character.Role,
            character.Version,
            character.CreatedAt);

        return Results.Created($"/api/characters/{character.Id}", response);
    }
}
