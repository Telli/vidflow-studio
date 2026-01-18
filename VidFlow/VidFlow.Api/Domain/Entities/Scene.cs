using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.Events;
using VidFlow.Api.Domain.Exceptions;

namespace VidFlow.Api.Domain.Entities;

/// <summary>
/// The atomic unit of creation containing script, shots, status, version, and runtime budget.
/// </summary>
public class Scene
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string NarrativeGoal { get; private set; } = string.Empty;
    public string EmotionalBeat { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public string TimeOfDay { get; private set; } = string.Empty;
    public SceneStatus Status { get; private set; }
    public int RuntimeEstimateSeconds { get; private set; }
    public int RuntimeTargetSeconds { get; private set; }
    public string Script { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public bool IsLocked { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public string? LockedBy { get; private set; }

    public List<string> CharacterNames { get; private set; } = [];
    public ICollection<Shot> Shots { get; private set; } = [];
    public ICollection<AgentProposal> Proposals { get; private set; } = [];

    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Scene() { }

    public static Scene Create(
        Guid projectId,
        string number,
        string title,
        string narrativeGoal,
        string emotionalBeat,
        string location,
        string timeOfDay,
        int runtimeTargetSeconds,
        IEnumerable<string>? characterNames = null)
    {
        var scene = new Scene
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Number = number,
            Title = title,
            NarrativeGoal = narrativeGoal,
            EmotionalBeat = emotionalBeat,
            Location = location,
            TimeOfDay = timeOfDay,
            Status = SceneStatus.Draft,
            RuntimeTargetSeconds = runtimeTargetSeconds,
            RuntimeEstimateSeconds = 0,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CharacterNames = characterNames?.ToList() ?? []
        };

        scene._domainEvents.Add(new SceneCreated(scene.Id, projectId, number, title));
        return scene;
    }

    public void Update(
        string? title = null,
        string? narrativeGoal = null,
        string? emotionalBeat = null,
        string? location = null,
        string? timeOfDay = null,
        string? script = null,
        IEnumerable<string>? characterNames = null)
    {
        if (IsCurrentlyLocked())
            throw new ConcurrentModificationException(Id);

        if (Status != SceneStatus.Draft)
            throw new SceneNotEditableException(Id, Status);

        if (title != null) Title = title;
        if (narrativeGoal != null) NarrativeGoal = narrativeGoal;
        if (emotionalBeat != null) EmotionalBeat = emotionalBeat;
        if (location != null) Location = location;
        if (timeOfDay != null) TimeOfDay = timeOfDay;
        if (script != null) Script = script;
        if (characterNames != null)
        {
            CharacterNames.Clear();
            CharacterNames.AddRange(characterNames);
        }

        Version++;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new SceneUpdated(Id, Version));
    }

    public void SubmitForReview()
    {
        if (IsCurrentlyLocked())
            throw new ConcurrentModificationException(Id);

        if (Status != SceneStatus.Draft)
            throw new InvalidStatusTransitionException(Status, SceneStatus.Review);

        Status = SceneStatus.Review;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new SceneSubmittedForReview(Id));
    }

    public void Approve(string approvedBy)
    {
        if (IsCurrentlyLocked())
            throw new ConcurrentModificationException(Id);

        if (Status != SceneStatus.Review)
            throw new InvalidStatusTransitionException(Status, SceneStatus.Approved);

        Status = SceneStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new SceneApproved(Id, approvedBy, Version));
    }

    public void RequestRevision(string feedback, string requestedBy)
    {
        if (IsCurrentlyLocked())
            throw new ConcurrentModificationException(Id);

        if (Status != SceneStatus.Review)
            throw new InvalidStatusTransitionException(Status, SceneStatus.Draft);

        Status = SceneStatus.Draft;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new SceneRevisionRequested(Id, feedback, requestedBy));
    }

    public static bool IsValidTransition(SceneStatus from, SceneStatus to) => (from, to) switch
    {
        (SceneStatus.Draft, SceneStatus.Review) => true,
        (SceneStatus.Review, SceneStatus.Approved) => true,
        (SceneStatus.Review, SceneStatus.Draft) => true,
        _ => false
    };

    public void RecalculateRuntimeEstimate()
    {
        RuntimeEstimateSeconds = Shots.Sum(s => s.GetDurationSeconds());
    }

    public bool TryAcquireLock(string lockedBy, TimeSpan duration)
    {
        if (IsLocked && LockedUntil > DateTime.UtcNow)
            return false;

        IsLocked = true;
        LockedBy = lockedBy;
        LockedUntil = DateTime.UtcNow.Add(duration);
        return true;
    }

    public void ReleaseLock()
    {
        IsLocked = false;
        LockedBy = null;
        LockedUntil = null;
    }

    public bool IsCurrentlyLocked() => IsLocked && LockedUntil > DateTime.UtcNow;

    public void ClearDomainEvents() => _domainEvents.Clear();
}
