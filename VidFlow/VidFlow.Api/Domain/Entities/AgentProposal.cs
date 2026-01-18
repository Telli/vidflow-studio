using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Domain.Entities;

/// <summary>
/// A non-destructive suggestion from an AI agent with structured diffs.
/// Agents produce proposals only and never mutate application state.
/// </summary>
public class AgentProposal
{
    public Guid Id { get; private set; }
    public Guid SceneId { get; private set; }
    public Scene Scene { get; private set; } = null!;
    public AgentRole Role { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string Rationale { get; private set; } = string.Empty;
    public int RuntimeImpactSeconds { get; private set; }
    public string Diff { get; private set; } = string.Empty;
    public ProposalStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int TokensUsed { get; private set; }
    public decimal CostUsd { get; private set; }

    private AgentProposal() { }

    public static AgentProposal Create(
        Guid sceneId,
        AgentRole role,
        string summary,
        string rationale,
        int runtimeImpactSeconds,
        string diff,
        int tokensUsed = 0,
        decimal costUsd = 0)
    {
        return new AgentProposal
        {
            Id = Guid.NewGuid(),
            SceneId = sceneId,
            Role = role,
            Summary = summary,
            Rationale = rationale,
            RuntimeImpactSeconds = runtimeImpactSeconds,
            Diff = diff,
            Status = ProposalStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            TokensUsed = tokensUsed,
            CostUsd = costUsd
        };
    }

    public void Apply() => Status = ProposalStatus.Applied;
    public void Dismiss() => Status = ProposalStatus.Dismissed;
}
