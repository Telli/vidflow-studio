using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.Events;
using VidFlow.Api.Domain.Exceptions;
using VidFlow.Api.Tests.Properties.Generators;

namespace VidFlow.Api.Tests.Properties;

/// <summary>
/// Property-based tests for scene status state machine.
/// Feature: vidflow-backend-integration, Property 15: Scene Status State Machine
/// Validates: Requirements 5.4
/// </summary>
public class SceneStatusStateMachineProperties
{
    /// <summary>
    /// Property 15.1: For any scene in Draft status, submitting for review SHALL transition to Review status.
    /// Valid transition: Draft → Review
    /// Validates: Requirements 5.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Draft_Scene_Can_Transition_To_Review()
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

                // Scene starts in Draft status
                var initialStatus = scene.Status == SceneStatus.Draft;

                // Transition to Review
                scene.SubmitForReview();

                var finalStatus = scene.Status == SceneStatus.Review;
                var eventEmitted = scene.DomainEvents.Any(e => e is SceneSubmittedForReview);

                return initialStatus && finalStatus && eventEmitted;
            });
    }

    /// <summary>
    /// Property 15.2: For any scene in Review status, approving SHALL transition to Approved status.
    /// Valid transition: Review → Approved
    /// Validates: Requirements 5.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Review_Scene_Can_Transition_To_Approved()
    {
        return Prop.ForAll(
            EntityGenerators.SceneCreationParamsArb(),
            ArbMap.Default.ArbFor<NonEmptyString>(),
            (SceneCreationParams input, NonEmptyString approver) =>
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

                // Move to Review first
                scene.SubmitForReview();
                var reviewStatus = scene.Status == SceneStatus.Review;

                // Transition to Approved
                scene.Approve(approver.Get);

                var finalStatus = scene.Status == SceneStatus.Approved;
                var approvedBySet = scene.ApprovedBy == approver.Get;
                var approvedAtSet = scene.ApprovedAt.HasValue;
                var eventEmitted = scene.DomainEvents.Any(e => e is SceneApproved);

                return reviewStatus && finalStatus && approvedBySet && approvedAtSet && eventEmitted;
            });
    }

    /// <summary>
    /// Property 15.3: For any scene in Review status, requesting revision SHALL transition back to Draft status.
    /// Valid transition: Review → Draft
    /// Validates: Requirements 5.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Review_Scene_Can_Transition_To_Draft_Via_Revision()
    {
        return Prop.ForAll(
            EntityGenerators.SceneCreationParamsArb(),
            ArbMap.Default.ArbFor<NonEmptyString>(),
            ArbMap.Default.ArbFor<NonEmptyString>(),
            (SceneCreationParams input, NonEmptyString feedback, NonEmptyString requestedBy) =>
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

                // Move to Review first
                scene.SubmitForReview();
                var reviewStatus = scene.Status == SceneStatus.Review;

                // Request revision (back to Draft)
                scene.RequestRevision(feedback.Get, requestedBy.Get);

                var finalStatus = scene.Status == SceneStatus.Draft;
                var eventEmitted = scene.DomainEvents.Any(e => e is SceneRevisionRequested);

                return reviewStatus && finalStatus && eventEmitted;
            });
    }

    /// <summary>
    /// Property 15.4: For any scene in Draft status, attempting to approve directly SHALL be rejected.
    /// Invalid transition: Draft → Approved (must go through Review)
    /// Validates: Requirements 5.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Draft_Scene_Cannot_Transition_Directly_To_Approved()
    {
        return Prop.ForAll(
            EntityGenerators.SceneCreationParamsArb(),
            ArbMap.Default.ArbFor<NonEmptyString>(),
            (SceneCreationParams input, NonEmptyString approver) =>
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

                // Scene starts in Draft status
                var initialStatus = scene.Status == SceneStatus.Draft;

                // Attempt to approve directly (should throw)
                var threwException = false;
                try
                {
                    scene.Approve(approver.Get);
                }
                catch (InvalidStatusTransitionException)
                {
                    threwException = true;
                }

                // Status should remain Draft
                var statusUnchanged = scene.Status == SceneStatus.Draft;

                return initialStatus && threwException && statusUnchanged;
            });
    }

    /// <summary>
    /// Property 15.5: For any scene in Approved status, no further transitions SHALL be allowed.
    /// Invalid transitions from Approved state.
    /// Validates: Requirements 5.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Approved_Scene_Cannot_Transition_To_Any_Other_Status()
    {
        return Prop.ForAll(
            EntityGenerators.SceneCreationParamsArb(),
            EntityGenerators.ApprovedSceneTransitionParamsArb(),
            (SceneCreationParams input, ApprovedSceneTransitionParams transitionParams) =>
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

                // Move to Approved state
                scene.SubmitForReview();
                scene.Approve(transitionParams.Approver);
                var approvedStatus = scene.Status == SceneStatus.Approved;

                // Attempt to submit for review again (should throw)
                var submitThrew = false;
                try
                {
                    scene.SubmitForReview();
                }
                catch (InvalidStatusTransitionException)
                {
                    submitThrew = true;
                }

                // Attempt to request revision (should throw)
                var revisionThrew = false;
                try
                {
                    scene.RequestRevision(transitionParams.Feedback, transitionParams.RequestedBy);
                }
                catch (InvalidStatusTransitionException)
                {
                    revisionThrew = true;
                }

                // Status should remain Approved
                var statusUnchanged = scene.Status == SceneStatus.Approved;

                return approvedStatus && submitThrew && revisionThrew && statusUnchanged;
            });
    }

    /// <summary>
    /// Property 15.6: The static IsValidTransition method SHALL correctly identify all valid and invalid transitions.
    /// Validates: Requirements 5.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property IsValidTransition_Correctly_Identifies_Valid_Transitions()
    {
        return Prop.ForAll(
            Gen.Elements(SceneStatus.Draft, SceneStatus.Review, SceneStatus.Approved).ToArbitrary(),
            Gen.Elements(SceneStatus.Draft, SceneStatus.Review, SceneStatus.Approved).ToArbitrary(),
            (SceneStatus from, SceneStatus to) =>
            {
                var isValid = Scene.IsValidTransition(from, to);

                // Define expected valid transitions
                var expectedValid = (from, to) switch
                {
                    (SceneStatus.Draft, SceneStatus.Review) => true,
                    (SceneStatus.Review, SceneStatus.Approved) => true,
                    (SceneStatus.Review, SceneStatus.Draft) => true,
                    _ => false
                };

                return isValid == expectedValid;
            });
    }

    /// <summary>
    /// Property 15.7: For any scene in Draft status, requesting revision SHALL be rejected.
    /// Invalid transition: Draft → Draft via revision (must be in Review first)
    /// Validates: Requirements 5.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Draft_Scene_Cannot_Request_Revision()
    {
        return Prop.ForAll(
            EntityGenerators.SceneCreationParamsArb(),
            ArbMap.Default.ArbFor<NonEmptyString>(),
            ArbMap.Default.ArbFor<NonEmptyString>(),
            (SceneCreationParams input, NonEmptyString feedback, NonEmptyString requestedBy) =>
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

                // Scene starts in Draft status
                var initialStatus = scene.Status == SceneStatus.Draft;

                // Attempt to request revision (should throw)
                var threwException = false;
                try
                {
                    scene.RequestRevision(feedback.Get, requestedBy.Get);
                }
                catch (InvalidStatusTransitionException)
                {
                    threwException = true;
                }

                // Status should remain Draft
                var statusUnchanged = scene.Status == SceneStatus.Draft;

                return initialStatus && threwException && statusUnchanged;
            });
    }
}
