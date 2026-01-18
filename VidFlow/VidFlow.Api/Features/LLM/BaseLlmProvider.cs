using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VidFlow.Api.Features.LLM;

public abstract class BaseLlmProvider
{
    protected readonly ILogger<BaseLlmProvider> _logger;
    protected readonly LlmProviderConfig _config;
    protected readonly HttpClient _httpClient;
    protected readonly JsonSerializerOptions _jsonOptions;

    protected BaseLlmProvider(
        ILogger<BaseLlmProvider> logger,
        IOptions<LlmProviderConfig> config,
        HttpClient httpClient)
    {
        _logger = logger;
        _config = config.Value;
        _httpClient = httpClient;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    protected async Task<LlmResponse> ExecuteWithRetryAsync(
        Func<Task<LlmResponse>> operation,
        CancellationToken ct,
        int maxRetries = 3)
    {
        var attempt = 0;
        var baseDelay = TimeSpan.FromMilliseconds(1000);

        while (attempt < maxRetries)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries - 1 && IsTransientError(ex))
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Attempt {Attempt} failed, retrying in {Delay}ms", attempt, delay.TotalMilliseconds);
                await Task.Delay(delay, ct);
            }
        }

        // Final attempt - let exception propagate
        return await operation();
    }

    private static bool IsTransientError(Exception ex)
    {
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               (ex is System.Net.Http.HttpRequestException httpEx && 
                (httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                 httpEx.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                 httpEx.StatusCode == System.Net.HttpStatusCode.RequestTimeout));
    }

    protected int EstimateTokenCount(string text)
    {
        // Simple estimation: ~4 characters per token for English
        return (int)Math.Ceiling(text.Length / 4.0);
    }
}
