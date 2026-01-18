namespace VidFlow.Api.Domain.ValueObjects;

/// <summary>
/// Represents an entry in the stitch plan for final rendering.
/// </summary>
public record StitchPlanEntry(
    Guid SceneId,
    int Order,
    string? TransitionType,
    string? TransitionNotes,
    string? AudioNotes);
