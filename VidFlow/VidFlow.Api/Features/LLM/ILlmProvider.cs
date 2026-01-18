namespace VidFlow.Api.Features.LLM;

public interface ILlmProvider
{
    string ProviderName { get; }
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct);
}

public class LlmRequest
{
    public string Prompt { get; set; } = "";
    public string SystemPrompt { get; set; } = "";
    public decimal Temperature { get; set; } = 0.7m;
    public int MaxTokens { get; set; } = 2000;
    public string Model { get; set; } = "";

    public LlmRequest() { }
    
    public LlmRequest(string prompt) : this() => Prompt = prompt;
    
    public LlmRequest(string prompt, string systemPrompt, decimal temperature, int maxTokens, string model) 
        : this() 
    { 
        Prompt = prompt;
        SystemPrompt = systemPrompt;
        Temperature = temperature;
        MaxTokens = maxTokens;
        Model = model;
    }
}

public record LlmResponse(
    string Content,
    int TokensUsed,
    decimal CostUsd,
    string Model,
    DateTime ProcessedAt);

public record LlmProviderConfig(
    string ApiKey,
    string BaseUrl,
    string DefaultModel,
    decimal CostPerToken);
