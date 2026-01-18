using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VidFlow.Api.Features.LLM.Providers;

public class GeminiProvider : BaseLlmProvider, ILlmProvider
{
    private const string GeminiApiPathTemplate = "models/{model}:generateContent";

    public string ProviderName => "Gemini";

    public GeminiProvider(
        ILogger<GeminiProvider> logger,
        IOptions<LlmProviderConfig> config,
        HttpClient httpClient) : base(logger, config, httpClient)
    {
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var requestedModel = string.IsNullOrWhiteSpace(request.Model) ? _config.DefaultModel : request.Model;
            var model = requestedModel.Contains("gemini", StringComparison.OrdinalIgnoreCase)
                ? requestedModel
                : _config.DefaultModel;
            var url = GeminiApiPathTemplate.Replace("{model}", model);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = request.SystemPrompt + "\n\n" + request.Prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = request.Temperature,
                    maxOutputTokens = request.MaxTokens
                }
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            requestMessage.Headers.TryAddWithoutValidation("x-goog-api-key", _config.ApiKey);

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var completionResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson, _jsonOptions);

            if (completionResponse?.Candidates?.Length > 0)
            {
                var candidate = completionResponse.Candidates[0];
                var contentObj = candidate.Content;
                var contentParts = contentObj?.Parts?.FirstOrDefault();
                var contentText = contentParts?.Text ?? "";
                var tokensUsed = completionResponse.UsageMetadata?.TotalTokenCount ?? EstimateTokenCount(contentText);
                var cost = CalculateCost(model, tokensUsed);

                return new LlmResponse(
                    contentText,
                    tokensUsed,
                    cost,
                    model,
                    DateTime.UtcNow);
            }

            throw new InvalidOperationException("Invalid response from Gemini API");
        }, ct);
    }

    private decimal CalculateCost(string model, int tokens)
    {
        return model.ToLowerInvariant() switch
        {
            "gemini-pro" or "gemini-1.0-pro" => tokens * 0.0000005m,
            "gemini-pro-vision" or "gemini-1.0-pro-vision" => tokens * 0.0000025m,
            "gemini-1.5-flash" or "gemini-1.5-flash-8b" => tokens * 0.000000075m,
            "gemini-1.5-pro" or "gemini-1.5-pro-latest" => tokens * 0.00000125m,
            _ => _config.CostPerToken * tokens
        };
    }

    private record GeminiResponse(
        GeminiCandidate[] Candidates,
        GeminiUsageMetadata? UsageMetadata);

    private record GeminiCandidate(
        GeminiContent Content);

    private record GeminiContent(
        GeminiPart[] Parts);

    private record GeminiPart(
        string Text);

    private record GeminiUsageMetadata(
        int TotalTokenCount);
}
