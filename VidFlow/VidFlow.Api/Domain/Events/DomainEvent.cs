using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Domain.Events;

/// <summary>
/// Base record for all domain events.
/// </summary>
public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string EmittedBy { get; init; } = "human";
}

// Project Events
public record ProjectCreated(Guid ProjectId, string Title, string Logline, int RuntimeTarget) : DomainEvent;
public record ProjectUpdated(Guid ProjectId, string Title, string Logline, int RuntimeTarget) : DomainEvent;

// Scene Events
public record SceneCreated(Guid SceneId, Guid ProjectId, string Number, string Title) : DomainEvent;
public record SceneUpdated(Guid SceneId, int NewVersion) : DomainEvent;
public record SceneSubmittedForReview(Guid SceneId) : DomainEvent;
public record SceneApproved(Guid SceneId, string ApprovedBy, int Version) : DomainEvent;
public record SceneRevisionRequested(Guid SceneId, string Feedback, string RequestedBy) : DomainEvent;
public record SceneRendered(Guid SceneId, Guid ArtifactId, int Version) : DomainEvent;

// Agent Events
public record AgentProposalCreated(Guid ProposalId, Guid SceneId, AgentRole Role, string Summary) : DomainEvent;
public record AgentRunFailed(Guid SceneId, AgentRole Role, string Error) : DomainEvent;

// Render Events
public record FinalRenderCompleted(Guid ProjectId, Guid ArtifactId) : DomainEvent;

// Story Bible Events
public record StoryBibleVersionCreated(Guid StoryBibleId, Guid ProjectId, int Version) : DomainEvent;

// Character Events
public record CharacterUpdated(Guid CharacterId, Guid ProjectId, int Version) : DomainEvent;
