using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.ValueObjects;

namespace VidFlow.Api.Features.Characters;

public static class UpdateCharacter
{
    public record Request(
        string? Name = null,
        string? Role = null,
        string? Archetype = null,
        string? Age = null,
        string? Description = null,
        string? Backstory = null,
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
        DateTime UpdatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/characters/{characterId}", Handler)
           .WithName("UpdateCharacter")
           .WithTags("Characters");
    }

    private static async Task<IResult> Handler(
        Guid characterId,
        Request request,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var character = await db.Characters.FindAsync([characterId], ct);
        if (character is null)
            return Results.NotFound($"Character with ID {characterId} not found.");

        // Check for duplicate name if name is being changed
        if (request.Name != null && request.Name != character.Name)
        {
            var existingCharacter = await db.Characters
                .FirstOrDefaultAsync(c => c.ProjectId == character.ProjectId && c.Name == request.Name, ct);
            if (existingCharacter != null)
                return Results.BadRequest($"Character with name '{request.Name}' already exists in this project.");
        }

        // Convert relationships
        var relationships = request.Relationships?.Select(r => 
            new CharacterRelationship(r.Name, r.Type, r.Note));

        // Update character
        character.Update(
            request.Name,
            request.Role,
            request.Archetype,
            request.Age,
            request.Description,
            request.Backstory,
            request.Traits,
            relationships);

        // Append domain events to event store
        foreach (var evt in character.DomainEvents)
        {
            db.EventStore.Add(EventStoreEntry.FromDomainEvent(evt, character.ProjectId, character.Id));
        }
        character.ClearDomainEvents();

        await db.SaveChangesAsync(ct);

        var response = new Response(
            character.Id,
            character.Name,
            character.Role,
            character.Version,
            character.UpdatedAt);

        return Results.Ok(response);
    }
}
