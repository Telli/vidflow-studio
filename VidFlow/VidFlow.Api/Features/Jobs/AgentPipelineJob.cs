using Hangfire;
using Hangfire.Server;
using VidFlow.Api.Features.Agents;

namespace VidFlow.Api.Features.Jobs;

/// <summary>
/// Hangfire job for running the agent pipeline on a scene.
/// Supports automatic retries and job tracking via PerformContext.
/// </summary>
public class AgentPipelineJob
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AgentPipelineJob(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Runs the agent pipeline for a scene.
    /// Configured with automatic retry (3 attempts) and 30-second delay between retries.
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task RunAsync(Guid sceneId, PerformContext? context)
    {
        var jobId = context?.BackgroundJob?.Id;

        using var scope = _scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<AgentRunner>();
        var result = await runner.RunPipelineAsync(sceneId, CancellationToken.None, jobId);

        if (!result.Success)
        {
            // Include budget info in error message for frontend detection
            var errorMessage = result.ErrorMessage ?? "Agent pipeline failed";
            throw new InvalidOperationException(errorMessage);
        }
    }
}
