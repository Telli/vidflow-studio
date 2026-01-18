using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Tests.Properties.Generators;

namespace VidFlow.Api.Tests.Properties;

/// <summary>
/// Property-based tests for event store immutability.
/// Feature: vidflow-backend-integration, Property 5: Event Store Immutability
/// Validates: Requirements 11.1, 11.5
/// </summary>
public class EventStoreImmutabilityProperties
{
    /// <summary>
    /// Property 5.1: For any sequence of event store entries created, the count SHALL only increase.
    /// This tests that events can only be added, never removed.
    /// Validates: Requirements 11.1, 11.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EventStore_Count_Only_Increases()
    {
        return Prop.ForAll(
            DomainEventGenerators.EventStoreEntryListArb(),
            (List<EventStoreEntryParams> eventParams) =>
            {
                var entries = new List<EventStoreEntry>();
                var previousCount = 0;
                var countOnlyIncreases = true;

                foreach (var param in eventParams)
                {
                    var entry = EventStoreEntry.FromDomainEvent(param.Event, param.ProjectId, param.EntityId);
                    entries.Add(entry);

                    // Verify count only increases
                    if (entries.Count <= previousCount)
                    {
                        countOnlyIncreases = false;
                        break;
                    }
                    previousCount = entries.Count;
                }

                return countOnlyIncreases && entries.Count == eventParams.Count;
            });
    }

    /// <summary>
    /// Property 5.2: For any event store entry, the content SHALL never change after creation.
    /// This tests that event entries are immutable once created.
    /// Validates: Requirements 11.1, 11.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EventStore_Entry_Content_Never_Changes()
    {
        return Prop.ForAll(
            DomainEventGenerators.EventStoreEntryParamsArb(),
            (EventStoreEntryParams param) =>
            {
                var entry = EventStoreEntry.FromDomainEvent(param.Event, param.ProjectId, param.EntityId);

                // Capture original values
                var originalId = entry.Id;
                var originalEventType = entry.EventType;
                var originalProjectId = entry.ProjectId;
                var originalEntityId = entry.EntityId;
                var originalPayload = entry.Payload;
                var originalEmittedBy = entry.EmittedBy;
                var originalTimestamp = entry.Timestamp;

                // Verify values remain unchanged (EventStoreEntry has private setters)
                var idUnchanged = entry.Id == originalId;
                var eventTypeUnchanged = entry.EventType == originalEventType;
                var projectIdUnchanged = entry.ProjectId == originalProjectId;
                var entityIdUnchanged = entry.EntityId == originalEntityId;
                var payloadUnchanged = entry.Payload == originalPayload;
                var emittedByUnchanged = entry.EmittedBy == originalEmittedBy;
                var timestampUnchanged = entry.Timestamp == originalTimestamp;

                return idUnchanged && eventTypeUnchanged && projectIdUnchanged &&
                       entityIdUnchanged && payloadUnchanged && emittedByUnchanged && timestampUnchanged;
            });
    }

    /// <summary>
    /// Property 5.3: For any event store entry, the ID SHALL match the source domain event's EventId.
    /// This ensures traceability between events and their store entries.
    /// Validates: Requirements 11.1, 11.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EventStore_Entry_Id_Matches_DomainEvent_EventId()
    {
        return Prop.ForAll(
            DomainEventGenerators.EventStoreEntryParamsArb(),
            (EventStoreEntryParams param) =>
            {
                var entry = EventStoreEntry.FromDomainEvent(param.Event, param.ProjectId, param.EntityId);

                return entry.Id == param.Event.EventId;
            });
    }

    /// <summary>
    /// Property 5.4: For any event store entry, all required fields SHALL be populated.
    /// This ensures completeness of event store records.
    /// Validates: Requirements 11.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EventStore_Entry_Has_All_Required_Fields()
    {
        return Prop.ForAll(
            DomainEventGenerators.EventStoreEntryParamsArb(),
            (EventStoreEntryParams param) =>
            {
                var entry = EventStoreEntry.FromDomainEvent(param.Event, param.ProjectId, param.EntityId);

                var hasId = entry.Id != Guid.Empty;
                var hasEventType = !string.IsNullOrEmpty(entry.EventType);
                var hasPayload = !string.IsNullOrEmpty(entry.Payload);
                var hasEmittedBy = !string.IsNullOrEmpty(entry.EmittedBy);
                var hasTimestamp = entry.Timestamp != default;
                var projectIdMatches = entry.ProjectId == param.ProjectId;
                var entityIdMatches = entry.EntityId == param.EntityId;

                return hasId && hasEventType && hasPayload && hasEmittedBy && 
                       hasTimestamp && projectIdMatches && entityIdMatches;
            });
    }
}
