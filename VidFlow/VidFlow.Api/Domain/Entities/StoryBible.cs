using VidFlow.Api.Domain.Events;

namespace VidFlow.Api.Domain.Entities;

/// <summary>
/// Versioned creative constraints document containing themes, world rules, tone, visual style, and pacing rules.
/// </summary>
public class StoryBible
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Themes { get; private set; } = string.Empty;
    public string WorldRules { get; private set; } = string.Empty;
    public string Tone { get; private set; } = string.Empty;
    public string VisualStyle { get; private set; } = string.Empty;
    public string PacingRules { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private StoryBible() { }

    public static StoryBible Create(
        Guid projectId,
        string themes,
        string worldRules,
        string tone,
        string visualStyle,
        string pacingRules)
    {
        var storyBible = new StoryBible
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Themes = themes,
            WorldRules = worldRules,
            Tone = tone,
            VisualStyle = visualStyle,
            PacingRules = pacingRules,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };

        storyBible._domainEvents.Add(new StoryBibleVersionCreated(storyBible.Id, projectId, 1));
        return storyBible;
    }

    public void Update(
        string? themes = null,
        string? worldRules = null,
        string? tone = null,
        string? visualStyle = null,
        string? pacingRules = null)
    {
        if (themes != null) Themes = themes;
        if (worldRules != null) WorldRules = worldRules;
        if (tone != null) Tone = tone;
        if (visualStyle != null) VisualStyle = visualStyle;
        if (pacingRules != null) PacingRules = pacingRules;

        Version++;
        _domainEvents.Add(new StoryBibleVersionCreated(Id, ProjectId, Version));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
