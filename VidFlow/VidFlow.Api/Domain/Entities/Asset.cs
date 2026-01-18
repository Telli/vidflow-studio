using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Domain.Entities;

public class Asset
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? SceneId { get; private set; }
    public Guid? ShotId { get; private set; }
    public AssetType Type { get; private set; }
    public AssetStatus Status { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Prompt { get; private set; }
    public string? Provider { get; private set; }
    public string? Url { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private Asset() { }

    public static Asset CreateStoryboardPlaceholder(
        Guid projectId,
        Guid sceneId,
        Guid shotId,
        string name,
        string? prompt,
        string placeholderUrl)
    {
        return new Asset
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SceneId = sceneId,
            ShotId = shotId,
            Type = AssetType.StoryboardImage,
            Status = AssetStatus.Completed,
            Name = name,
            Prompt = prompt,
            Provider = "placeholder",
            Url = placeholderUrl,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessing(string provider)
    {
        Status = AssetStatus.Processing;
        Provider = provider;
        ErrorMessage = null;
    }

    public void MarkCompleted(string url)
    {
        Status = AssetStatus.Completed;
        Url = url;
        ErrorMessage = null;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = AssetStatus.Failed;
        ErrorMessage = error;
        CompletedAt = DateTime.UtcNow;
    }
}
