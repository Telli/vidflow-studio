using Hangfire;
using VidFlow.Api.Features.Render;

namespace VidFlow.Api.Features.Jobs;

/// <summary>
/// Hangfire job for processing render requests.
/// Supports automatic retries for transient failures.
/// </summary>
public class RenderJobProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RenderJobProcessor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Processes a render job.
    /// Configured with automatic retry (3 attempts) with exponential backoff.
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task RunAsync(Guid renderJobId)
    {
        using var scope = _scopeFactory.CreateScope();
        var renderService = scope.ServiceProvider.GetRequiredService<RenderService>();
        await renderService.ProcessRenderJobAsync(renderJobId, CancellationToken.None);
    }
}
