using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Domain.Entities;

/// <summary>
/// Represents a video rendering job for animatics, scenes, or final film.
/// </summary>
public class RenderJob
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? SceneId { get; private set; }
    public RenderType Type { get; private set; }
    public RenderStatus Status { get; private set; }
    public int ProgressPercent { get; private set; }
    public string? ArtifactPath { get; private set; }
    public int? SourceVersion { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private RenderJob() { }

    public static RenderJob CreateAnimatic(Guid projectId, Guid sceneId, int sceneVersion)
    {
        return new RenderJob
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SceneId = sceneId,
            Type = RenderType.Animatic,
            Status = RenderStatus.Queued,
            ProgressPercent = 0,
            SourceVersion = sceneVersion,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static RenderJob CreateSceneRender(Guid projectId, Guid sceneId, int sceneVersion)
    {
        return new RenderJob
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SceneId = sceneId,
            Type = RenderType.Scene,
            Status = RenderStatus.Queued,
            ProgressPercent = 0,
            SourceVersion = sceneVersion,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static RenderJob CreateFinalRender(Guid projectId)
    {
        return new RenderJob
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SceneId = null,
            Type = RenderType.Final,
            Status = RenderStatus.Queued,
            ProgressPercent = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Start()
    {
        Status = RenderStatus.Processing;
        StartedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(int percent)
    {
        ProgressPercent = Math.Clamp(percent, 0, 100);
    }

    public void Complete(string artifactPath)
    {
        Status = RenderStatus.Completed;
        ProgressPercent = 100;
        ArtifactPath = artifactPath;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = RenderStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}
