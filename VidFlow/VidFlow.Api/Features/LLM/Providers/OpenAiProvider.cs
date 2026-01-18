using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VidFlow.Api.Features.LLM.Providers;

public class OpenAiProvider : BaseLlmProvider, ILlmProvider
{
    private const string OpenAiApiPath = "chat/completions";

    public string ProviderName => "OpenAI";

    public OpenAiProvider(
        ILogger<OpenAiProvider> logger,
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
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.Prompt }
                },
                temperature = request.Temperature,
                max_tokens = request.MaxTokens
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, OpenAiApiPath)
            {
                Content = content
            };
            requestMessage.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_config.ApiKey}");

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var completionResponse = JsonSerializer.Deserialize<OpenAiResponse>(responseJson, _jsonOptions);

            if (completionResponse?.Choices?.Length > 0)
            {
                var choice = completionResponse.Choices[0];
                var tokensUsed = completionResponse.Usage?.TotalTokens ?? EstimateTokenCount(choice.Message.Content);
                var cost = CalculateCost(model, tokensUsed);

                return new LlmResponse(
                    choice.Message.Content,
                    tokensUsed,
                    cost,
                    model,
                    DateTime.UtcNow);
            }

            throw new InvalidOperationException("Invalid response from OpenAI API");
        }, ct);
    }

    private decimal CalculateCost(string model, int tokens)
    {
        return model.ToLowerInvariant() switch
        {
            "gpt-4" or "gpt-4-0314" => tokens * 0.00003m,
            "gpt-4-32k" => tokens * 0.00006m,
            "gpt-3.5-turbo" or "gpt-3.5-turbo-16k" => tokens * 0.000002m,
            _ => _config.CostPerToken * tokens
        };
    }

    private record OpenAiResponse(
        OpenAiChoice[] Choices,
        OpenAiUsage? Usage);

    private record OpenAiChoice(
        OpenAiMessage Message);

    private record OpenAiMessage(
        string Role,
        string Content);

    private record OpenAiUsage(
        int PromptTokens,
        int CompletionTokens,
        int TotalTokens);
}
