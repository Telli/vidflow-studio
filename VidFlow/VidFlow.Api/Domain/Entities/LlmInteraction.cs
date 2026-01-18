using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Domain.Entities;

/// <summary>
/// Persisted record of an LLM API call for auditability, debugging, and cost tracking.
/// Stores the full request/response with timing and cost information.
/// </summary>
public class LlmInteraction
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The project this interaction belongs to (for cost aggregation).
    /// </summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// Optional scene ID if this interaction is scene-specific.
    /// </summary>
    public Guid? SceneId { get; private set; }

    /// <summary>
    /// The agent role that made this call (Writer, Director, etc.).
    /// </summary>
    public AgentRole? AgentRole { get; private set; }

    /// <summary>
    /// The Hangfire job ID if this was part of a background job.
    /// </summary>
    public string? JobId { get; private set; }

    /// <summary>
    /// The LLM provider used (openai, anthropic, gemini).
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// The specific model used (e.g., gpt-4o, claude-3-5-sonnet-20241022).
    /// </summary>
    public string Model { get; private set; } = string.Empty;

    /// <summary>
    /// The system prompt sent to the LLM.
    /// </summary>
    public string SystemPrompt { get; private set; } = string.Empty;

    /// <summary>
    /// The user prompt sent to the LLM.
    /// </summary>
    public string Prompt { get; private set; } = string.Empty;

    /// <summary>
    /// Temperature parameter used.
    /// </summary>
    public decimal Temperature { get; private set; }

    /// <summary>
    /// Max tokens parameter used.
    /// </summary>
    public int MaxTokens { get; private set; }

    /// <summary>
    /// The raw response content from the LLM.
    /// </summary>
    public string? ResponseContent { get; private set; }

    /// <summary>
    /// Number of input tokens used.
    /// </summary>
    public int InputTokens { get; private set; }

    /// <summary>
    /// Number of output tokens used.
    /// </summary>
    public int OutputTokens { get; private set; }

    /// <summary>
    /// Total tokens used (input + output).
    /// </summary>
    public int TotalTokens { get; private set; }

    /// <summary>
    /// Estimated cost in USD.
    /// </summary>
    public decimal CostUsd { get; private set; }

    /// <summary>
    /// Whether the request succeeded.
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// Error message if the request failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Time taken to complete the request in milliseconds.
    /// </summary>
    public int DurationMs { get; private set; }

    /// <summary>
    /// When the interaction was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the response was received.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    private LlmInteraction() { }

    public static LlmInteraction Create(
        Guid projectId,
        Guid? sceneId,
        AgentRole? agentRole,
        string? jobId,
        string provider,
        string model,
        string systemPrompt,
        string prompt,
        decimal temperature,
        int maxTokens)
    {
        return new LlmInteraction
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SceneId = sceneId,
            AgentRole = agentRole,
            JobId = jobId,
            Provider = provider,
            Model = model,
            SystemPrompt = systemPrompt,
            Prompt = prompt,
            Temperature = temperature,
            MaxTokens = maxTokens,
            Success = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void RecordSuccess(
        string responseContent,
        int inputTokens,
        int outputTokens,
        decimal costUsd,
        int durationMs)
    {
        ResponseContent = responseContent;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        TotalTokens = inputTokens + outputTokens;
        CostUsd = costUsd;
        DurationMs = durationMs;
        Success = true;
        CompletedAt = DateTime.UtcNow;
    }

    public void RecordFailure(string errorMessage, int durationMs)
    {
        ErrorMessage = errorMessage;
        DurationMs = durationMs;
        Success = false;
        CompletedAt = DateTime.UtcNow;
    }
}
