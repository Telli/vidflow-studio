namespace VidFlow.Api.Domain.Entities;

/// <summary>
/// A single camera setup within a scene with type, duration, description, and camera intent.
/// </summary>
public class Shot
{
    public Guid Id { get; private set; }
    public Guid SceneId { get; private set; }
    public int Number { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Duration { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Camera { get; private set; } = string.Empty;

    private Shot() { }

    public static Shot Create(
        Guid sceneId,
        int number,
        string type,
        string duration,
        string description,
        string camera)
    {
        return new Shot
        {
            Id = Guid.NewGuid(),
            SceneId = sceneId,
            Number = number,
            Type = type,
            Duration = duration,
            Description = description,
            Camera = camera
        };
    }

    public void Update(
        string? type = null,
        string? duration = null,
        string? description = null,
        string? camera = null)
    {
        if (type != null) Type = type;
        if (duration != null) Duration = duration;
        if (description != null) Description = description;
        if (camera != null) Camera = camera;
    }

    public void SetNumber(int number) => Number = number;

    public int GetDurationSeconds()
    {
        if (string.IsNullOrEmpty(Duration))
            return 0;

        var trimmed = Duration.TrimEnd('s', 'S');
        if (!int.TryParse(trimmed, out var seconds))
            return 0;

        return Math.Max(0, seconds);
    }
}
