using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VidFlow.Api.Features.LLM.Providers;

namespace VidFlow.Api.Features.LLM;

/// <summary>
/// Factory implementation that creates LLM providers on demand.
/// Validates API key availability and supports dynamic provider selection.
/// </summary>
public class LlmProviderFactory : ILlmProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly LlmSettings _settings;
    private readonly Dictionary<string, Func<ILlmProvider>> _providerFactories;
    private readonly List<string> _availableProviders;

    public LlmProviderFactory(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        IOptions<LlmSettings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _settings = settings.Value;
        _availableProviders = new List<string>();
        _providerFactories = new Dictionary<string, Func<ILlmProvider>>(StringComparer.OrdinalIgnoreCase);

        RegisterProviders();
    }

    public string DefaultProviderName => _settings.DefaultProvider;

    public IReadOnlyList<string> AvailableProviders => _availableProviders;

    public ILlmProvider GetDefault() => Get(_settings.DefaultProvider);

    public ILlmProvider Get(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            providerName = _settings.DefaultProvider;

        if (!_providerFactories.TryGetValue(providerName, out var factory))
            throw new InvalidOperationException($"Unknown LLM provider: {providerName}. Available: {string.Join(", ", _availableProviders)}");

        return factory();
    }

    private void RegisterProviders()
    {
        // OpenAI
        if (!string.IsNullOrWhiteSpace(_settings.OpenAi.ApiKey))
        {
            _availableProviders.Add("openai");
            _providerFactories["openai"] = () => CreateOpenAiProvider();
        }

        // Anthropic
        if (!string.IsNullOrWhiteSpace(_settings.Anthropic.ApiKey))
        {
            _availableProviders.Add("anthropic");
            _providerFactories["anthropic"] = () => CreateAnthropicProvider();
        }

        // Gemini
        if (!string.IsNullOrWhiteSpace(_settings.Gemini.ApiKey))
        {
            _availableProviders.Add("gemini");
            _providerFactories["gemini"] = () => CreateGeminiProvider();
        }

        if (_availableProviders.Count == 0)
            throw new InvalidOperationException("No LLM providers configured. Set at least one API key (LLM:OpenAi:ApiKey, LLM:Anthropic:ApiKey, or LLM:Gemini:ApiKey).");
    }

    private ILlmProvider CreateOpenAiProvider()
    {
        var config = new LlmProviderConfig(
            _settings.OpenAi.ApiKey,
            "https://api.openai.com/v1/",
            _settings.OpenAi.DefaultModel,
            _settings.OpenAi.CostPerOutputToken);

        var httpClient = _httpClientFactory.CreateClient("OpenAI");
        var logger = _loggerFactory.CreateLogger<OpenAiProvider>();
        return new OpenAiProvider(logger, Options.Create(config), httpClient);
    }

    private ILlmProvider CreateAnthropicProvider()
    {
        var config = new LlmProviderConfig(
            _settings.Anthropic.ApiKey,
            "https://api.anthropic.com/v1/",
            _settings.Anthropic.DefaultModel,
            _settings.Anthropic.CostPerOutputToken);

        var httpClient = _httpClientFactory.CreateClient("Anthropic");
        var logger = _loggerFactory.CreateLogger<AnthropicProvider>();
        return new AnthropicProvider(logger, Options.Create(config), httpClient);
    }

    private ILlmProvider CreateGeminiProvider()
    {
        var config = new LlmProviderConfig(
            _settings.Gemini.ApiKey,
            "https://generativelanguage.googleapis.com/v1beta/",
            _settings.Gemini.DefaultModel,
            _settings.Gemini.CostPerOutputToken);

        var httpClient = _httpClientFactory.CreateClient("Gemini");
        var logger = _loggerFactory.CreateLogger<GeminiProvider>();
        return new GeminiProvider(logger, Options.Create(config), httpClient);
    }
}
