using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Tests.Properties.Generators;

namespace VidFlow.Api.Tests.Properties;

/// <summary>
/// Property-based tests for entity creation completeness.
/// Feature: vidflow-backend-integration, Property 1: Entity Creation Completeness
/// Validates: Requirements 1.1, 3.1, 4.1, 7.1, 8.1
/// </summary>
public class EntityCreationCompletenessProperties
{
    /// <summary>
    /// Property 1.1: For any valid Project creation request, the persisted entity SHALL contain
    /// all required fields with correct values matching the input.
    /// Validates: Requirements 1.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Project_Creation_Preserves_All_Input_Fields()
    {
        return Prop.ForAll(
            EntityGenerators.ProjectCreationParamsArb(),
            (ProjectCreationParams input) =>
            {
                var project = Project.Create(input.Title, input.Logline, input.RuntimeTargetSeconds);

                var titleMatches = project.Title == input.Title;
                var loglineMatches = project.Logline == input.Logline;
                var runtimeMatches = project.RuntimeTargetSeconds == input.RuntimeTargetSeconds;
                var idGenerated = project.Id != Guid.Empty;
                var statusIsIdeation = project.Status == ProjectStatus.Ideation;
                var createdAtSet = project.CreatedAt <= DateTime.UtcNow;
                var updatedAtSet = project.UpdatedAt <= DateTime.UtcNow;
                var domainEventEmitted = project.DomainEvents.Count == 1;

                return titleMatches && loglineMatches && runtimeMatches && idGenerated &&
                       statusIsIdeation && createdAtSet && updatedAtSet && domainEventEmitted;
            });
    }

    /// <summary>
    /// Property 1.2: For any valid Scene creation request, the persisted entity SHALL contain
    /// all required fields with correct values matching the input.
    /// Validates: Requirements 4.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Scene_Creation_Preserves_All_Input_Fields()
    {
        return Prop.ForAll(
            EntityGenerators.SceneCreationParamsArb(),
            (SceneCreationParams input) =>
            {
                var scene = Scene.Create(
                    input.ProjectId,
                    input.Number,
                    input.Title,
                    input.NarrativeGoal,
                    input.EmotionalBeat,
                    input.Location,
                    input.TimeOfDay,
                    input.RuntimeTargetSeconds,
                    input.CharacterNames);

                var projectIdMatches = scene.ProjectId == input.ProjectId;
                var numberMatches = scene.Number == input.Number;
                var titleMatches = scene.Title == input.Title;
                var narrativeGoalMatches = scene.NarrativeGoal == input.NarrativeGoal;
                var emotionalBeatMatches = scene.EmotionalBeat == input.EmotionalBeat;
                var locationMatches = scene.Location == input.Location;
                var timeOfDayMatches = scene.TimeOfDay == input.TimeOfDay;
                var runtimeMatches = scene.RuntimeTargetSeconds == input.RuntimeTargetSeconds;
                var characterNamesMatch = scene.CharacterNames.SequenceEqual(input.CharacterNames);
                var idGenerated = scene.Id != Guid.Empty;
                var statusIsDraft = scene.Status == SceneStatus.Draft;
                var versionIsOne = scene.Version == 1;
                var createdAtSet = scene.CreatedAt <= DateTime.UtcNow;
                var domainEventEmitted = scene.DomainEvents.Count == 1;

                return projectIdMatches && numberMatches && titleMatches && narrativeGoalMatches &&
                       emotionalBeatMatches && locationMatches && timeOfDayMatches && runtimeMatches &&
                       characterNamesMatch && idGenerated && statusIsDraft && versionIsOne &&
                       createdAtSet && domainEventEmitted;
            });
    }

    /// <summary>
    /// Property 1.3: For any valid Character creation request, the persisted entity SHALL contain
    /// all required fields with correct values matching the input.
    /// Validates: Requirements 3.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Character_Creation_Preserves_All_Input_Fields()
    {
        return Prop.ForAll(
            EntityGenerators.CharacterCreationParamsArb(),
            (CharacterCreationParams input) =>
            {
                var character = Character.Create(
                    input.ProjectId,
                    input.Name,
                    input.Role,
                    input.Archetype,
                    input.Age,
                    input.Description,
                    input.Backstory,
                    input.Traits);

                var projectIdMatches = character.ProjectId == input.ProjectId;
                var nameMatches = character.Name == input.Name;
                var roleMatches = character.Role == input.Role;
                var archetypeMatches = character.Archetype == input.Archetype;
                var ageMatches = character.Age == input.Age;
                var descriptionMatches = character.Description == input.Description;
                var backstoryMatches = character.Backstory == input.Backstory;
                var traitsMatch = character.Traits.SequenceEqual(input.Traits);
                var idGenerated = character.Id != Guid.Empty;
                var versionIsOne = character.Version == 1;
                var createdAtSet = character.CreatedAt <= DateTime.UtcNow;

                return projectIdMatches && nameMatches && roleMatches && archetypeMatches &&
                       ageMatches && descriptionMatches && backstoryMatches && traitsMatch &&
                       idGenerated && versionIsOne && createdAtSet;
            });
    }

    /// <summary>
    /// Property 1.4: For any valid Shot creation request, the persisted entity SHALL contain
    /// all required fields with correct values matching the input.
    /// Validates: Requirements 8.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Shot_Creation_Preserves_All_Input_Fields()
    {
        return Prop.ForAll(
            EntityGenerators.ShotCreationParamsArb(),
            (ShotCreationParams input) =>
            {
                var shot = Shot.Create(
                    input.SceneId,
                    input.Number,
                    input.Type,
                    input.Duration,
                    input.Description,
                    input.Camera);

                var sceneIdMatches = shot.SceneId == input.SceneId;
                var numberMatches = shot.Number == input.Number;
                var typeMatches = shot.Type == input.Type;
                var durationMatches = shot.Duration == input.Duration;
                var descriptionMatches = shot.Description == input.Description;
                var cameraMatches = shot.Camera == input.Camera;
                var idGenerated = shot.Id != Guid.Empty;

                return sceneIdMatches && numberMatches && typeMatches && durationMatches &&
                       descriptionMatches && cameraMatches && idGenerated;
            });
    }

    /// <summary>
    /// Property 1.5: For any valid AgentProposal creation request, the persisted entity SHALL contain
    /// all required fields with correct values matching the input.
    /// Validates: Requirements 7.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AgentProposal_Creation_Preserves_All_Input_Fields()
    {
        return Prop.ForAll(
            EntityGenerators.ProposalCreationParamsArb(),
            (ProposalCreationParams input) =>
            {
                var proposal = AgentProposal.Create(
                    input.SceneId,
                    input.Role,
                    input.Summary,
                    input.Rationale,
                    input.RuntimeImpactSeconds,
                    input.Diff,
                    input.TokensUsed,
                    input.CostUsd);

                var sceneIdMatches = proposal.SceneId == input.SceneId;
                var roleMatches = proposal.Role == input.Role;
                var summaryMatches = proposal.Summary == input.Summary;
                var rationaleMatches = proposal.Rationale == input.Rationale;
                var runtimeImpactMatches = proposal.RuntimeImpactSeconds == input.RuntimeImpactSeconds;
                var diffMatches = proposal.Diff == input.Diff;
                var tokensUsedMatches = proposal.TokensUsed == input.TokensUsed;
                var costUsdMatches = proposal.CostUsd == input.CostUsd;
                var idGenerated = proposal.Id != Guid.Empty;
                var statusIsPending = proposal.Status == ProposalStatus.Pending;
                var createdAtSet = proposal.CreatedAt <= DateTime.UtcNow;

                return sceneIdMatches && roleMatches && summaryMatches && rationaleMatches &&
                       runtimeImpactMatches && diffMatches && tokensUsedMatches && costUsdMatches &&
                       idGenerated && statusIsPending && createdAtSet;
            });
    }
}
