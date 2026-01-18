using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.ValueObjects;

namespace VidFlow.Api.Features.Characters;

public static class GetCharacters
{
    public record Response(
        Guid Id,
        string Name,
        string Role,
        string Archetype,
        string Age,
        string Description,
        string Backstory,
        List<string> Traits,
        List<CharacterRelationshipDto> Relationships,
        int Version,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public record CharacterRelationshipDto(
        string Name,
        string Type,
        string Note);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId}/characters", Handler)
           .WithName("GetCharacters")
           .WithTags("Characters");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        // Validate project exists
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return Results.NotFound($"Project with ID {projectId} not found.");

        var characters = await db.Characters
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var response = characters.Select(c => new Response(
            c.Id,
            c.Name,
            c.Role,
            c.Archetype,
            c.Age,
            c.Description,
            c.Backstory,
            c.Traits,
            c.Relationships.Select(r => new CharacterRelationshipDto(r.Name, r.Type, r.Note)).ToList(),
            c.Version,
            c.CreatedAt,
            c.UpdatedAt));

        return Results.Ok(response);
    }
}
