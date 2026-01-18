using System.Diagnostics;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Features.LLM;

/// <summary>
/// Wraps an LLM provider to persist all interactions to the database.
/// Provides full auditability of LLM requests/responses.
/// </summary>
public class TrackedLlmProvider : ILlmProvider
{
    private readonly ILlmProvider _inner;
    private readonly VidFlowDbContext _db;
    private readonly LlmInteractionContext _context;

    public string ProviderName => _inner.ProviderName;

    public TrackedLlmProvider(ILlmProvider inner, VidFlowDbContext db, LlmInteractionContext context)
    {
        _inner = inner;
        _db = db;
        _context = context;
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct)
    {
        var interaction = LlmInteraction.Create(
            _context.ProjectId,
            _context.SceneId,
            _context.AgentRole,
            _context.JobId,
            _inner.ProviderName,
            request.Model,
            request.SystemPrompt,
            request.Prompt,
            request.Temperature,
            request.MaxTokens);

        _db.LlmInteractions.Add(interaction);
        await _db.SaveChangesAsync(ct);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await _inner.CompleteAsync(request, ct);
            stopwatch.Stop();

            // Estimate input tokens (provider may not return this)
            var estimatedInputTokens = EstimateTokenCount(request.SystemPrompt + request.Prompt);

            interaction.RecordSuccess(
                response.Content,
                estimatedInputTokens,
                response.TokensUsed,
                response.CostUsd,
                (int)stopwatch.ElapsedMilliseconds);

            await _db.SaveChangesAsync(ct);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            interaction.RecordFailure(ex.Message, (int)stopwatch.ElapsedMilliseconds);
            await _db.SaveChangesAsync(ct);
            throw;
        }
    }

    private static int EstimateTokenCount(string text)
    {
        // Simple estimation: ~4 characters per token
        return string.IsNullOrEmpty(text) ? 0 : (text.Length + 3) / 4;
    }
}

/// <summary>
/// Context for tracking LLM interactions.
/// Contains metadata about the request context for auditing.
/// </summary>
public class LlmInteractionContext
{
    public Guid ProjectId { get; set; }
    public Guid? SceneId { get; set; }
    public AgentRole? AgentRole { get; set; }
    public string? JobId { get; set; }

    public static LlmInteractionContext ForAgentRun(Guid projectId, Guid sceneId, AgentRole role, string? jobId = null)
    {
        return new LlmInteractionContext
        {
            ProjectId = projectId,
            SceneId = sceneId,
            AgentRole = role,
            JobId = jobId
        };
    }
}

/// <summary>
/// Service for creating tracked LLM providers with context.
/// </summary>
public interface ITrackedLlmProviderFactory
{
    /// <summary>
    /// Creates a tracked provider with the specified context.
    /// </summary>
    ILlmProvider CreateTracked(string? providerName, LlmInteractionContext context);

    /// <summary>
    /// Gets the LLM settings for role overrides.
    /// </summary>
    LlmSettings Settings { get; }
}

public class TrackedLlmProviderFactory : ITrackedLlmProviderFactory
{
    private readonly ILlmProviderFactory _providerFactory;
    private readonly VidFlowDbContext _db;
    private readonly LlmSettings _settings;

    public LlmSettings Settings => _settings;

    public TrackedLlmProviderFactory(
        ILlmProviderFactory providerFactory,
        VidFlowDbContext db,
        Microsoft.Extensions.Options.IOptions<LlmSettings> settings)
    {
        _providerFactory = providerFactory;
        _db = db;
        _settings = settings.Value;
    }

    public ILlmProvider CreateTracked(string? providerName, LlmInteractionContext context)
    {
        var provider = string.IsNullOrWhiteSpace(providerName)
            ? _providerFactory.GetDefault()
            : _providerFactory.Get(providerName);

        return new TrackedLlmProvider(provider, _db, context);
    }
}
