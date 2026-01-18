using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.LLM;

namespace VidFlow.Api.Features.Agents;

public interface ICreativeAgent
{
    AgentRole Role { get; }
    Task<AgentProposal?> AnalyzeAsync(AgentContext context, CancellationToken ct);
}

/// <summary>
/// Context passed to each agent during pipeline execution.
/// Contains scene data, project context, and LLM configuration.
/// </summary>
public record AgentContext(
    Scene Scene,
    VidFlow.Api.Domain.Entities.StoryBible? StoryBible,
    IEnumerable<Character> Characters,
    IEnumerable<AgentProposal> PriorProposals)
{
    /// <summary>
    /// The tracked LLM provider for this agent run.
    /// Automatically persists interactions to the database.
    /// </summary>
    public ILlmProvider? LlmProvider { get; init; }

    /// <summary>
    /// Configuration overrides for this agent's LLM calls.
    /// </summary>
    public AgentLlmConfig? LlmConfig { get; init; }

    /// <summary>
    /// The Hangfire job ID for this pipeline run (for tracking).
    /// </summary>
    public string? JobId { get; init; }
}

/// <summary>
/// LLM configuration for an agent run.
/// </summary>
public record AgentLlmConfig
{
    /// <summary>
    /// Provider name override (openai, anthropic, gemini).
    /// If null, uses the default provider.
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>
    /// Model name override.
    /// If null, uses the provider's default model.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Temperature override.
    /// If null, uses the agent's default temperature.
    /// </summary>
    public decimal? Temperature { get; init; }

    /// <summary>
    /// Max tokens override.
    /// If null, uses the agent's default max tokens.
    /// </summary>
    public int? MaxTokens { get; init; }

    public static AgentLlmConfig Default => new();

    public static AgentLlmConfig FromRoleOverride(RoleModelConfig? roleConfig)
    {
        if (roleConfig is null)
            return Default;

        return new AgentLlmConfig
        {
            Provider = roleConfig.Provider,
            Model = roleConfig.Model,
            Temperature = roleConfig.Temperature,
            MaxTokens = roleConfig.MaxTokens
        };
    }
}

public record AgentPipelineResult
{
    public bool Success { get; init; }
    public IReadOnlyList<AgentProposal> Proposals { get; init; } = [];
    public AgentRole? FailedAtRole { get; init; }
    public string? ErrorMessage { get; init; }

    public static AgentPipelineResult Succeeded(IEnumerable<AgentProposal> proposals)
        => new() { Success = true, Proposals = proposals.ToList() };

    public static AgentPipelineResult Failed(AgentRole role, string error)
        => new() { Success = false, FailedAtRole = role, ErrorMessage = error };
}
