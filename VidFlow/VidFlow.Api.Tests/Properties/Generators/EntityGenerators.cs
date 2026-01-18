using FsCheck;
using FsCheck.Fluent;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.Events;
using VidFlow.Api.Domain.ValueObjects;

namespace VidFlow.Api.Tests.Properties.Generators;

/// <summary>
/// FsCheck generators for domain entities.
/// </summary>
public static class EntityGenerators
{
    private static Gen<string> NonEmptyStringGen() =>
        ArbMap.Default.GeneratorFor<NonEmptyString>().Select(s => s.Get);

    private static Gen<Guid> GuidGen() =>
        ArbMap.Default.GeneratorFor<Guid>();

    /// <summary>
    /// Generates valid project creation parameters.
    /// </summary>
    public static Arbitrary<ProjectCreationParams> ProjectCreationParamsArb() =>
        (from title in NonEmptyStringGen()
         from logline in NonEmptyStringGen()
         from runtimeTarget in Gen.Choose(300, 1200)
         select new ProjectCreationParams(title, logline, runtimeTarget))
        .ToArbitrary();

    /// <summary>
    /// Generates valid scene creation parameters.
    /// </summary>
    public static Arbitrary<SceneCreationParams> SceneCreationParamsArb() =>
        (from projectId in GuidGen()
         from number in NonEmptyStringGen()
         from title in NonEmptyStringGen()
         from narrativeGoal in NonEmptyStringGen()
         from emotionalBeat in NonEmptyStringGen()
         from location in NonEmptyStringGen()
         from timeOfDay in Gen.Elements("Day", "Night", "Dawn", "Dusk")
         from runtimeTarget in Gen.Choose(10, 300)
         from characterCount in Gen.Choose(0, 5)
         from characterNames in Gen.ListOf(NonEmptyStringGen(), characterCount)
         select new SceneCreationParams(
             projectId,
             number,
             title,
             narrativeGoal,
             emotionalBeat,
             location,
             timeOfDay,
             runtimeTarget,
             characterNames.ToList()))
        .ToArbitrary();

    /// <summary>
    /// Generates valid character creation parameters.
    /// </summary>
    public static Arbitrary<CharacterCreationParams> CharacterCreationParamsArb() =>
        (from projectId in GuidGen()
         from name in NonEmptyStringGen()
         from role in Gen.Elements("Protagonist", "Antagonist", "Supporting", "Minor")
         from archetype in Gen.Elements("Hero", "Mentor", "Trickster", "Shadow", "Guardian")
         from age in Gen.Elements("20s", "30s", "40s", "50s", "60s", "Teen", "Child", "Elder")
         from description in NonEmptyStringGen()
         from backstory in NonEmptyStringGen()
         from traitCount in Gen.Choose(0, 5)
         from traits in Gen.ListOf(NonEmptyStringGen(), traitCount)
         select new CharacterCreationParams(
             projectId,
             name,
             role,
             archetype,
             age,
             description,
             backstory,
             traits.ToList()))
        .ToArbitrary();

    /// <summary>
    /// Generates valid shot creation parameters.
    /// </summary>
    public static Arbitrary<ShotCreationParams> ShotCreationParamsArb() =>
        (from sceneId in GuidGen()
         from number in Gen.Choose(1, 100)
         from type in Gen.Elements("Wide", "Medium", "Close-up", "Extreme Close-up", "Over-the-shoulder", "POV", "Establishing")
         from durationSeconds in Gen.Choose(1, 60)
         from description in NonEmptyStringGen()
         from camera in Gen.Elements("Static", "Pan", "Tilt", "Dolly", "Crane", "Handheld", "Steadicam")
         select new ShotCreationParams(
             sceneId,
             number,
             type,
             $"{durationSeconds}s",
             description,
             camera))
        .ToArbitrary();

    /// <summary>
    /// Generates valid agent proposal creation parameters.
    /// </summary>
    public static Arbitrary<ProposalCreationParams> ProposalCreationParamsArb() =>
        (from sceneId in GuidGen()
         from role in Gen.Elements(AgentRole.Writer, AgentRole.Director, AgentRole.Cinematographer, AgentRole.Editor, AgentRole.Producer)
         from summary in NonEmptyStringGen()
         from rationale in NonEmptyStringGen()
         from runtimeImpact in Gen.Choose(-30, 30)
         from diff in NonEmptyStringGen()
         from tokensUsed in Gen.Choose(0, 10000)
         from costUsd in Gen.Choose(0, 100).Select(c => c / 100m)
         select new ProposalCreationParams(
             sceneId,
             role,
             summary,
             rationale,
             runtimeImpact,
             diff,
             tokensUsed,
             costUsd))
        .ToArbitrary();

    /// <summary>
    /// Generates parameters for testing transitions from Approved scene status.
    /// </summary>
    public static Arbitrary<ApprovedSceneTransitionParams> ApprovedSceneTransitionParamsArb() =>
        (from approver in NonEmptyStringGen()
         from feedback in NonEmptyStringGen()
         from requestedBy in NonEmptyStringGen()
         select new ApprovedSceneTransitionParams(approver, feedback, requestedBy))
        .ToArbitrary();
}

/// <summary>
/// Parameters for creating a Project entity.
/// </summary>
public record ProjectCreationParams(string Title, string Logline, int RuntimeTargetSeconds);

/// <summary>
/// Parameters for creating a Scene entity.
/// </summary>
public record SceneCreationParams(
    Guid ProjectId,
    string Number,
    string Title,
    string NarrativeGoal,
    string EmotionalBeat,
    string Location,
    string TimeOfDay,
    int RuntimeTargetSeconds,
    List<string> CharacterNames);

/// <summary>
/// Parameters for creating a Character entity.
/// </summary>
public record CharacterCreationParams(
    Guid ProjectId,
    string Name,
    string Role,
    string Archetype,
    string Age,
    string Description,
    string Backstory,
    List<string> Traits);

/// <summary>
/// Parameters for creating a Shot entity.
/// </summary>
public record ShotCreationParams(
    Guid SceneId,
    int Number,
    string Type,
    string Duration,
    string Description,
    string Camera);

/// <summary>
/// Parameters for creating an AgentProposal entity.
/// </summary>
public record ProposalCreationParams(
    Guid SceneId,
    AgentRole Role,
    string Summary,
    string Rationale,
    int RuntimeImpactSeconds,
    string Diff,
    int TokensUsed,
    decimal CostUsd);


/// <summary>
/// Parameters for testing transitions from Approved scene status.
/// </summary>
public record ApprovedSceneTransitionParams(
    string Approver,
    string Feedback,
    string RequestedBy);


/// <summary>
/// Parameters for creating an EventStoreEntry.
/// </summary>
public record EventStoreEntryParams(
    DomainEvent Event,
    Guid? ProjectId,
    Guid? EntityId);

/// <summary>
/// Extension methods for domain event generators.
/// </summary>
public static class DomainEventGenerators
{
    private static Gen<string> NonEmptyStringGen() =>
        ArbMap.Default.GeneratorFor<NonEmptyString>().Select(s => s.Get);

    private static Gen<Guid> GuidGen() =>
        ArbMap.Default.GeneratorFor<Guid>();

    /// <summary>
    /// Generates a random ProjectCreated domain event.
    /// </summary>
    public static Gen<ProjectCreated> ProjectCreatedGen() =>
        from projectId in GuidGen()
        from title in NonEmptyStringGen()
        from logline in NonEmptyStringGen()
        from runtimeTarget in Gen.Choose(300, 1200)
        select new ProjectCreated(projectId, title, logline, runtimeTarget);

    /// <summary>
    /// Generates a random SceneCreated domain event.
    /// </summary>
    public static Gen<SceneCreated> SceneCreatedGen() =>
        from sceneId in GuidGen()
        from projectId in GuidGen()
        from number in NonEmptyStringGen()
        from title in NonEmptyStringGen()
        select new SceneCreated(sceneId, projectId, number, title);

    /// <summary>
    /// Generates a random SceneUpdated domain event.
    /// </summary>
    public static Gen<SceneUpdated> SceneUpdatedGen() =>
        from sceneId in GuidGen()
        from newVersion in Gen.Choose(1, 100)
        select new SceneUpdated(sceneId, newVersion);

    /// <summary>
    /// Generates a random SceneApproved domain event.
    /// </summary>
    public static Gen<SceneApproved> SceneApprovedGen() =>
        from sceneId in GuidGen()
        from approvedBy in NonEmptyStringGen()
        from version in Gen.Choose(1, 100)
        select new SceneApproved(sceneId, approvedBy, version);

    /// <summary>
    /// Generates a random domain event of any type.
    /// </summary>
    public static Gen<DomainEvent> AnyDomainEventGen() =>
        Gen.OneOf(
            ProjectCreatedGen().Select(e => (DomainEvent)e),
            SceneCreatedGen().Select(e => (DomainEvent)e),
            SceneUpdatedGen().Select(e => (DomainEvent)e),
            SceneApprovedGen().Select(e => (DomainEvent)e));

    /// <summary>
    /// Generates valid EventStoreEntry creation parameters.
    /// </summary>
    public static Arbitrary<EventStoreEntryParams> EventStoreEntryParamsArb() =>
        (from evt in AnyDomainEventGen()
         from projectId in GuidGen()
         from entityId in GuidGen()
         select new EventStoreEntryParams(evt, projectId, entityId))
        .ToArbitrary();

    /// <summary>
    /// Generates a list of domain events for testing event store operations.
    /// </summary>
    public static Arbitrary<List<EventStoreEntryParams>> EventStoreEntryListArb() =>
        (from count in Gen.Choose(1, 10)
         from events in Gen.ListOf(
             from evt in AnyDomainEventGen()
             from projectId in GuidGen()
             from entityId in GuidGen()
             select new EventStoreEntryParams(evt, projectId, entityId),
             count)
         select events.ToList())
        .ToArbitrary();
}
