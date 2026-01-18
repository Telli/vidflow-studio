using VidFlow.Api.Domain.Events;
using VidFlow.Api.Domain.ValueObjects;

namespace VidFlow.Api.Domain.Entities;

/// <summary>
/// A defined entity with voice rules, arc, appearance constraints, and relationships.
/// </summary>
public class Character
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string Archetype { get; private set; } = string.Empty;
    public string Age { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Backstory { get; private set; } = string.Empty;
    public List<string> Traits { get; private set; } = [];
    public List<CharacterRelationship> Relationships { get; private set; } = [];
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Character() { }

    public static Character Create(
        Guid projectId,
        string name,
        string role,
        string archetype,
        string age,
        string description,
        string backstory,
        IEnumerable<string>? traits = null,
        IEnumerable<CharacterRelationship>? relationships = null)
    {
        return new Character
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            Role = role,
            Archetype = archetype,
            Age = age,
            Description = description,
            Backstory = backstory,
            Traits = traits?.ToList() ?? [],
            Relationships = relationships?.ToList() ?? [],
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string? name = null,
        string? role = null,
        string? archetype = null,
        string? age = null,
        string? description = null,
        string? backstory = null,
        IEnumerable<string>? traits = null,
        IEnumerable<CharacterRelationship>? relationships = null)
    {
        if (name != null) Name = name;
        if (role != null) Role = role;
        if (archetype != null) Archetype = archetype;
        if (age != null) Age = age;
        if (description != null) Description = description;
        if (backstory != null) Backstory = backstory;

        if (traits != null)
        {
            Traits.Clear();
            Traits.AddRange(traits);
        }

        if (relationships != null)
        {
            Relationships.Clear();
            Relationships.AddRange(relationships);
        }

        Version++;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new CharacterUpdated(Id, ProjectId, Version));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
