namespace VidFlow.Api.Domain.Enums;

/// <summary>
/// Represents the status of a scene in the review workflow.
/// Valid transitions: Draft → Review → Approved, or Review → Draft (revision).
/// </summary>
public enum SceneStatus
{
    Draft,
    Review,
    Approved
}
