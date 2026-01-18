using VidFlow.Api.Domain.ValueObjects;

namespace VidFlow.Api.Domain.Entities;

/// <summary>
/// Ordered list of approved scenes with transitions and audio notes for final rendering.
/// </summary>
public class StitchPlan
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public List<StitchPlanEntry> Entries { get; private set; } = [];
    public int TotalRuntimeSeconds { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private StitchPlan() { }

    public static StitchPlan Create(Guid projectId)
    {
        return new StitchPlan
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            TotalRuntimeSeconds = 0,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddEntry(StitchPlanEntry entry)
    {
        var desiredOrder = entry.Order;
        var clampedOrder = Math.Clamp(desiredOrder, 1, Entries.Count + 1);
        var insertIndex = clampedOrder - 1;

        Entries.Insert(insertIndex, entry with { Order = clampedOrder });
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveEntry(Guid sceneId)
    {
        var entry = Entries.FirstOrDefault(e => e.SceneId == sceneId);
        if (entry != null)
        {
            Entries.Remove(entry);
            ReorderEntries();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetTransition(Guid sceneId, string transitionType, string? transitionNotes = null)
    {
        var index = Entries.FindIndex(e => e.SceneId == sceneId);
        if (index >= 0)
        {
            var existing = Entries[index];
            Entries[index] = existing with
            {
                TransitionType = transitionType,
                TransitionNotes = transitionNotes
            };
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetAudioNotes(Guid sceneId, string audioNotes)
    {
        var index = Entries.FindIndex(e => e.SceneId == sceneId);
        if (index >= 0)
        {
            var existing = Entries[index];
            Entries[index] = existing with { AudioNotes = audioNotes };
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void ReorderEntries()
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            Entries[i] = Entries[i] with { Order = i + 1 };
        }
    }

    public void SetTotalRuntime(int totalRuntimeSeconds)
    {
        TotalRuntimeSeconds = totalRuntimeSeconds;
        UpdatedAt = DateTime.UtcNow;
    }
}
