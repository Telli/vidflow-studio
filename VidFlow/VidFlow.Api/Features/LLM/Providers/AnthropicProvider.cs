using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VidFlow.Api.Features.LLM.Providers;

public class AnthropicProvider : BaseLlmProvider, ILlmProvider
{
    private const string AnthropicApiPath = "messages";

    public string ProviderName => "Anthropic";

    public AnthropicProvider(
        ILogger<AnthropicProvider> logger,
        IOptions<LlmProviderConfig> config,
        HttpClient httpClient) : base(logger, config, httpClient)
    {
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var model = string.IsNullOrWhiteSpace(request.Model) ? _config.DefaultModel : request.Model;

            var requestBody = new
            {
                model,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                system = request.SystemPrompt,
                messages = new[]
                {
                    new { role = "user", content = request.Prompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, AnthropicApiPath)
            {
                Content = content
            };
            requestMessage.Headers.TryAddWithoutValidation("x-api-key", _config.ApiKey);
            requestMessage.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var completionResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseJson, _jsonOptions);

            if (completionResponse?.Content?.Length > 0)
            {
                var contentItem = completionResponse.Content[0];
                var contentText = contentItem.Text;
                var tokensUsed = completionResponse.Usage?.InputTokens + completionResponse.Usage?.OutputTokens ?? EstimateTokenCount(contentText);
                var cost = CalculateCost(model, tokensUsed);

                return new LlmResponse(
                    contentText,
                    tokensUsed,
                    cost,
                    model,
                    DateTime.UtcNow);
            }

            throw new InvalidOperationException("Invalid response from Anthropic API");
        }, ct);
    }

    private decimal CalculateCost(string model, int tokens)
    {
        return model.ToLowerInvariant() switch
        {
            "claude-3-opus" or "claude-3-opus-20240229" => tokens * 0.000075m,
            "claude-3-sonnet" or "claude-3-sonnet-20240229" => tokens * 0.000015m,
            "claude-3-haiku" or "claude-3-haiku-20240307" => tokens * 0.0000025m,
            "claude-2.1" or "claude-2.0" => tokens * 0.000008m,
            _ => _config.CostPerToken * tokens
        };
    }

    private record AnthropicResponse(
        AnthropicContent[] Content,
        AnthropicUsage? Usage);

    private record AnthropicContent(
        string Type,
        string Text);

    private record AnthropicUsage(
        int InputTokens,
        int OutputTokens);
}
