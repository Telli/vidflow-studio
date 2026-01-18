using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Domain.Exceptions;

/// <summary>
/// Base class for all domain exceptions.
/// </summary>
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }

    protected DomainException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Thrown when attempting to edit a scene that is not in an editable state.
/// </summary>
public class SceneNotEditableException : DomainException
{
    public SceneNotEditableException(Guid sceneId, SceneStatus status)
        : base("SCENE_NOT_EDITABLE", $"Scene {sceneId} cannot be edited in status {status}") { }
}

/// <summary>
/// Thrown when attempting an invalid scene status transition.
/// </summary>
public class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(SceneStatus from, SceneStatus to)
        : base("INVALID_STATUS_TRANSITION", $"Cannot transition from {from} to {to}") { }
}

/// <summary>
/// Thrown when an operation requires an approved scene but the scene is not approved.
/// </summary>
public class SceneNotApprovedException : DomainException
{
    public SceneNotApprovedException(Guid sceneId)
        : base("SCENE_NOT_APPROVED", $"Scene {sceneId} must be approved for this operation") { }
}

/// <summary>
/// Thrown when an operation would exceed the budget cap.
/// </summary>
public class BudgetExceededException : DomainException
{
    public BudgetExceededException(Guid entityId, decimal currentCost, decimal budget)
        : base("BUDGET_EXCEEDED", $"Operation would exceed budget. Current: {currentCost}, Budget: {budget}") { }
}

/// <summary>
/// Thrown when attempting to create a character with a duplicate name in a project.
/// </summary>
public class DuplicateCharacterNameException : DomainException
{
    public DuplicateCharacterNameException(string name, Guid projectId)
        : base("DUPLICATE_CHARACTER_NAME", $"Character '{name}' already exists in project {projectId}") { }
}

/// <summary>
/// Thrown when attempting to modify a scene that is currently being processed by agents.
/// </summary>
public class ConcurrentModificationException : DomainException
{
    public ConcurrentModificationException(Guid sceneId)
        : base("CONCURRENT_MODIFICATION", $"Scene {sceneId} is currently being processed by agents") { }
}
