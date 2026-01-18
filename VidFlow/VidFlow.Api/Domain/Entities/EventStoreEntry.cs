using System.Text.Json;
using VidFlow.Api.Domain.Events;

namespace VidFlow.Api.Domain.Entities;

/// <summary>
/// Represents an entry in the append-only event store for auditability and replay.
/// This entity is immutable after creation - events are never modified or deleted.
/// </summary>
public class EventStoreEntry
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid? ProjectId { get; private set; }
    public Guid? EntityId { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public string EmittedBy { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }

    private EventStoreEntry() { }

    /// <summary>
    /// Creates an EventStoreEntry from a domain event.
    /// This is the only way to create an entry, ensuring all required fields are populated.
    /// </summary>
    public static EventStoreEntry FromDomainEvent(DomainEvent evt, Guid? projectId, Guid? entityId)
    {
        ArgumentNullException.ThrowIfNull(evt);

        return new EventStoreEntry
        {
            Id = evt.EventId,
            EventType = evt.GetType().Name,
            ProjectId = projectId,
            EntityId = entityId,
            Payload = JsonSerializer.Serialize(evt, evt.GetType()),
            EmittedBy = evt.EmittedBy,
            Timestamp = evt.Timestamp
        };
    }

    /// <summary>
    /// Deserializes the payload to a specific domain event type.
    /// Used for event replay and reconstruction.
    /// </summary>
    public T? DeserializePayload<T>() where T : DomainEvent
    {
        if (string.IsNullOrEmpty(Payload))
            return null;

        return JsonSerializer.Deserialize<T>(Payload);
    }

    /// <summary>
    /// Deserializes the payload to a domain event using the stored event type.
    /// Returns null if the event type cannot be resolved.
    /// </summary>
    public DomainEvent? DeserializePayload()
    {
        if (string.IsNullOrEmpty(Payload) || string.IsNullOrEmpty(EventType))
            return null;

        var eventType = GetEventType();
        if (eventType == null)
            return null;

        return JsonSerializer.Deserialize(Payload, eventType) as DomainEvent;
    }

    /// <summary>
    /// Gets the .NET Type for the stored event type name.
    /// </summary>
    private Type? GetEventType()
    {
        return EventType switch
        {
            nameof(ProjectCreated) => typeof(ProjectCreated),
            nameof(ProjectUpdated) => typeof(ProjectUpdated),
            nameof(SceneCreated) => typeof(SceneCreated),
            nameof(SceneUpdated) => typeof(SceneUpdated),
            nameof(SceneSubmittedForReview) => typeof(SceneSubmittedForReview),
            nameof(SceneApproved) => typeof(SceneApproved),
            nameof(SceneRevisionRequested) => typeof(SceneRevisionRequested),
            nameof(SceneRendered) => typeof(SceneRendered),
            nameof(AgentProposalCreated) => typeof(AgentProposalCreated),
            nameof(AgentRunFailed) => typeof(AgentRunFailed),
            nameof(FinalRenderCompleted) => typeof(FinalRenderCompleted),
            nameof(StoryBibleVersionCreated) => typeof(StoryBibleVersionCreated),
            nameof(CharacterUpdated) => typeof(CharacterUpdated),
            _ => null
        };
    }
}
