using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Events;
using VidFlow.Api.Tests.Properties.Generators;

namespace VidFlow.Api.Tests.Properties;

/// <summary>
/// Property-based tests for event replay round-trip.
/// Feature: vidflow-backend-integration, Property 6: Event Replay Round-Trip
/// Validates: Requirements 11.3
/// </summary>
public class EventReplayRoundTripProperties
{
    /// <summary>
    /// Property 6.1: For any ProjectCreated event, serializing to EventStoreEntry and deserializing
    /// SHALL produce an equivalent event with matching field values.
    /// Validates: Requirements 11.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProjectCreated_RoundTrip_Preserves_Data()
    {
        return Prop.ForAll(
            DomainEventGenerators.ProjectCreatedGen().ToArbitrary(),
            (ProjectCreated originalEvent) =>
            {
                var projectId = Guid.NewGuid();
                var entityId = originalEvent.ProjectId;

                // Serialize to EventStoreEntry
                var entry = EventStoreEntry.FromDomainEvent(originalEvent, projectId, entityId);

                // Deserialize back to domain event
                var deserializedEvent = entry.DeserializePayload<ProjectCreated>();

                if (deserializedEvent == null)
                    return false;

                // Verify all fields match
                var projectIdMatches = deserializedEvent.ProjectId == originalEvent.ProjectId;
                var titleMatches = deserializedEvent.Title == originalEvent.Title;
                var loglineMatches = deserializedEvent.Logline == originalEvent.Logline;
                var runtimeTargetMatches = deserializedEvent.RuntimeTarget == originalEvent.RuntimeTarget;
                var eventIdMatches = deserializedEvent.EventId == originalEvent.EventId;
                var timestampMatches = deserializedEvent.Timestamp == originalEvent.Timestamp;
                var emittedByMatches = deserializedEvent.EmittedBy == originalEvent.EmittedBy;

                return projectIdMatches && titleMatches && loglineMatches && runtimeTargetMatches &&
                       eventIdMatches && timestampMatches && emittedByMatches;
            });
    }

    /// <summary>
    /// Property 6.2: For any SceneCreated event, serializing to EventStoreEntry and deserializing
    /// SHALL produce an equivalent event with matching field values.
    /// Validates: Requirements 11.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SceneCreated_RoundTrip_Preserves_Data()
    {
        return Prop.ForAll(
            DomainEventGenerators.SceneCreatedGen().ToArbitrary(),
            (SceneCreated originalEvent) =>
            {
                var projectId = originalEvent.ProjectId;
                var entityId = originalEvent.SceneId;

                // Serialize to EventStoreEntry
                var entry = EventStoreEntry.FromDomainEvent(originalEvent, projectId, entityId);

                // Deserialize back to domain event
                var deserializedEvent = entry.DeserializePayload<SceneCreated>();

                if (deserializedEvent == null)
                    return false;

                // Verify all fields match
                var sceneIdMatches = deserializedEvent.SceneId == originalEvent.SceneId;
                var projectIdMatches = deserializedEvent.ProjectId == originalEvent.ProjectId;
                var numberMatches = deserializedEvent.Number == originalEvent.Number;
                var titleMatches = deserializedEvent.Title == originalEvent.Title;
                var eventIdMatches = deserializedEvent.EventId == originalEvent.EventId;
                var timestampMatches = deserializedEvent.Timestamp == originalEvent.Timestamp;

                return sceneIdMatches && projectIdMatches && numberMatches && titleMatches &&
                       eventIdMatches && timestampMatches;
            });
    }

    /// <summary>
    /// Property 6.3: For any SceneUpdated event, serializing to EventStoreEntry and deserializing
    /// SHALL produce an equivalent event with matching field values.
    /// Validates: Requirements 11.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SceneUpdated_RoundTrip_Preserves_Data()
    {
        return Prop.ForAll(
            DomainEventGenerators.SceneUpdatedGen().ToArbitrary(),
            (SceneUpdated originalEvent) =>
            {
                var projectId = Guid.NewGuid();
                var entityId = originalEvent.SceneId;

                // Serialize to EventStoreEntry
                var entry = EventStoreEntry.FromDomainEvent(originalEvent, projectId, entityId);

                // Deserialize back to domain event
                var deserializedEvent = entry.DeserializePayload<SceneUpdated>();

                if (deserializedEvent == null)
                    return false;

                // Verify all fields match
                var sceneIdMatches = deserializedEvent.SceneId == originalEvent.SceneId;
                var newVersionMatches = deserializedEvent.NewVersion == originalEvent.NewVersion;
                var eventIdMatches = deserializedEvent.EventId == originalEvent.EventId;
                var timestampMatches = deserializedEvent.Timestamp == originalEvent.Timestamp;

                return sceneIdMatches && newVersionMatches && eventIdMatches && timestampMatches;
            });
    }

    /// <summary>
    /// Property 6.4: For any SceneApproved event, serializing to EventStoreEntry and deserializing
    /// SHALL produce an equivalent event with matching field values.
    /// Validates: Requirements 11.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SceneApproved_RoundTrip_Preserves_Data()
    {
        return Prop.ForAll(
            DomainEventGenerators.SceneApprovedGen().ToArbitrary(),
            (SceneApproved originalEvent) =>
            {
                var projectId = Guid.NewGuid();
                var entityId = originalEvent.SceneId;

                // Serialize to EventStoreEntry
                var entry = EventStoreEntry.FromDomainEvent(originalEvent, projectId, entityId);

                // Deserialize back to domain event
                var deserializedEvent = entry.DeserializePayload<SceneApproved>();

                if (deserializedEvent == null)
                    return false;

                // Verify all fields match
                var sceneIdMatches = deserializedEvent.SceneId == originalEvent.SceneId;
                var approvedByMatches = deserializedEvent.ApprovedBy == originalEvent.ApprovedBy;
                var versionMatches = deserializedEvent.Version == originalEvent.Version;
                var eventIdMatches = deserializedEvent.EventId == originalEvent.EventId;
                var timestampMatches = deserializedEvent.Timestamp == originalEvent.Timestamp;

                return sceneIdMatches && approvedByMatches && versionMatches &&
                       eventIdMatches && timestampMatches;
            });
    }

    /// <summary>
    /// Property 6.5: For any domain event, deserializing using the generic DeserializePayload()
    /// method SHALL produce an event with the correct runtime type.
    /// Validates: Requirements 11.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Generic_Deserialization_Returns_Correct_Type()
    {
        return Prop.ForAll(
            DomainEventGenerators.EventStoreEntryParamsArb(),
            (EventStoreEntryParams param) =>
            {
                // Serialize to EventStoreEntry
                var entry = EventStoreEntry.FromDomainEvent(param.Event, param.ProjectId, param.EntityId);

                // Deserialize using generic method
                var deserializedEvent = entry.DeserializePayload();

                if (deserializedEvent == null)
                    return false;

                // Verify the type matches
                var typeMatches = deserializedEvent.GetType() == param.Event.GetType();
                var eventIdMatches = deserializedEvent.EventId == param.Event.EventId;

                return typeMatches && eventIdMatches;
            });
    }

    /// <summary>
    /// Property 6.6: For any domain event, the EventType stored in EventStoreEntry
    /// SHALL match the original event's type name.
    /// Validates: Requirements 11.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EventType_Matches_Original_Event_TypeName()
    {
        return Prop.ForAll(
            DomainEventGenerators.EventStoreEntryParamsArb(),
            (EventStoreEntryParams param) =>
            {
                var entry = EventStoreEntry.FromDomainEvent(param.Event, param.ProjectId, param.EntityId);

                return entry.EventType == param.Event.GetType().Name;
            });
    }
}
