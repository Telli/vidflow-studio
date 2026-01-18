using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.Events;

namespace VidFlow.Api.Domain.Entities;

/// <summary>
/// The top-level container for a film project.
/// </summary>
public class Project
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Logline { get; private set; } = string.Empty;
    public int RuntimeTargetSeconds { get; private set; }
    public ProjectStatus Status { get; private set; }
    public decimal BudgetCapUsd { get; private set; }
    public decimal CurrentSpendUsd { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<Scene> Scenes { get; private set; } = [];
    public ICollection<Character> Characters { get; private set; } = [];
    public StoryBible? StoryBible { get; private set; }
    public StitchPlan? StitchPlan { get; private set; }

    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Project() { }

    public static Project Create(string title, string logline, int runtimeTargetSeconds)
    {
        if (runtimeTargetSeconds < 300 || runtimeTargetSeconds > 1200)
            throw new ArgumentOutOfRangeException(nameof(runtimeTargetSeconds),
                "Runtime target must be between 300 and 1200 seconds.");

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = title,
            Logline = logline,
            RuntimeTargetSeconds = runtimeTargetSeconds,
            Status = ProjectStatus.Ideation,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        project._domainEvents.Add(new ProjectCreated(project.Id, title, logline, runtimeTargetSeconds));
        return project;
    }

    public void Update(string title, string logline, int runtimeTargetSeconds)
    {
        if (runtimeTargetSeconds < 300 || runtimeTargetSeconds > 1200)
            throw new ArgumentOutOfRangeException(nameof(runtimeTargetSeconds),
                "Runtime target must be between 300 and 1200 seconds.");

        Title = title;
        Logline = logline;
        RuntimeTargetSeconds = runtimeTargetSeconds;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new ProjectUpdated(Id, title, logline, runtimeTargetSeconds));
    }

    public int GetTotalRuntimeSeconds() => Scenes.Sum(s => s.RuntimeEstimateSeconds);
    public int GetSceneCount() => Scenes.Count;
    public int GetPendingReviewCount() => Scenes.Count(s => s.Status == SceneStatus.Review);

    public void SetBudgetCap(decimal budgetCapUsd)
    {
        if (budgetCapUsd < 0)
            throw new ArgumentOutOfRangeException(nameof(budgetCapUsd), "Budget cap cannot be negative.");
        
        BudgetCapUsd = budgetCapUsd;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSpend(decimal amountUsd)
    {
        if (amountUsd < 0)
            throw new ArgumentOutOfRangeException(nameof(amountUsd), "Spend amount cannot be negative.");
        
        CurrentSpendUsd += amountUsd;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool WouldExceedBudget(decimal additionalSpend)
    {
        if (BudgetCapUsd <= 0) return false; // No budget cap set
        return (CurrentSpendUsd + additionalSpend) > BudgetCapUsd;
    }

    public decimal GetRemainingBudget() => Math.Max(0, BudgetCapUsd - CurrentSpendUsd);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
