namespace VidFlow.Api.Features.LLM;

/// <summary>
/// Factory for creating LLM providers based on configuration.
/// Supports per-request provider selection with fallback to default.
/// </summary>
public interface ILlmProviderFactory
{
    /// <summary>
    /// Gets the default provider based on configuration.
    /// </summary>
    ILlmProvider GetDefault();

    /// <summary>
    /// Gets a specific provider by name (openai, anthropic, gemini).
    /// </summary>
    ILlmProvider Get(string providerName);

    /// <summary>
    /// Gets the default provider name from configuration.
    /// </summary>
    string DefaultProviderName { get; }

    /// <summary>
    /// Lists all available (configured) provider names.
    /// </summary>
    IReadOnlyList<string> AvailableProviders { get; }
}

/// <summary>
/// Configuration for LLM settings including defaults and per-role overrides.
/// </summary>
public class LlmSettings
{
    public string DefaultProvider { get; set; } = "openai";
    public string DefaultModel { get; set; } = "";
    public Dictionary<string, RoleModelConfig> RoleOverrides { get; set; } = new();
    public OpenAiSettings OpenAi { get; set; } = new();
    public AnthropicSettings Anthropic { get; set; } = new();
    public GeminiSettings Gemini { get; set; } = new();
}

public class RoleModelConfig
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public decimal? Temperature { get; set; }
    public int? MaxTokens { get; set; }
}

public class OpenAiSettings
{
    public string ApiKey { get; set; } = "";
    public string DefaultModel { get; set; } = "gpt-4o";
    public decimal CostPerInputToken { get; set; } = 0.0000025m;
    public decimal CostPerOutputToken { get; set; } = 0.00001m;
}

public class AnthropicSettings
{
    public string ApiKey { get; set; } = "";
    public string DefaultModel { get; set; } = "claude-3-5-sonnet-20241022";
    public decimal CostPerInputToken { get; set; } = 0.000003m;
    public decimal CostPerOutputToken { get; set; } = 0.000015m;
}

public class GeminiSettings
{
    public string ApiKey { get; set; } = "";
    public string DefaultModel { get; set; } = "gemini-1.5-pro";
    public decimal CostPerInputToken { get; set; } = 0.00000125m;
    public decimal CostPerOutputToken { get; set; } = 0.000005m;
}
