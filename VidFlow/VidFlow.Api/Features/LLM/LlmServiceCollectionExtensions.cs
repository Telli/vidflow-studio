using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using VidFlow.Api.Features.LLM;
using VidFlow.Api.Features.LLM.Providers;

namespace VidFlow.Api.Features.LLM;

public static class LlmServiceCollectionExtensions
{
    private const string OpenAiClientName = "OpenAI";
    private const string AnthropicClientName = "Anthropic";
    private const string GeminiClientName = "Gemini";

    public static IServiceCollection AddLlmProviders(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind LLM settings from configuration
        var llmSettings = new LlmSettings();
        configuration.GetSection("LLM").Bind(llmSettings);

        // Also check for environment variable overrides
        llmSettings.OpenAi.ApiKey = GetSetting(configuration, "LLM:OpenAi:ApiKey", "OPENAI_API_KEY") ?? llmSettings.OpenAi.ApiKey;
        llmSettings.Anthropic.ApiKey = GetSetting(configuration, "LLM:Anthropic:ApiKey", "ANTHROPIC_API_KEY") ?? llmSettings.Anthropic.ApiKey;
        llmSettings.Gemini.ApiKey = GetSetting(configuration, "LLM:Gemini:ApiKey", "GEMINI_API_KEY") ?? llmSettings.Gemini.ApiKey;
        llmSettings.DefaultProvider = GetSetting(configuration, "LLM:DefaultProvider", "LLM_DEFAULT_PROVIDER") ?? llmSettings.DefaultProvider;

        // Register settings
        services.Configure<LlmSettings>(opt =>
        {
            opt.DefaultProvider = llmSettings.DefaultProvider;
            opt.DefaultModel = llmSettings.DefaultModel;
            opt.RoleOverrides = llmSettings.RoleOverrides;
            opt.OpenAi = llmSettings.OpenAi;
            opt.Anthropic = llmSettings.Anthropic;
            opt.Gemini = llmSettings.Gemini;
        });

        // Register HTTP clients with longer timeout for LLM calls
        services.AddHttpClient(OpenAiClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddHttpClient(AnthropicClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/v1/");
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddHttpClient(GeminiClientName, client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        // Register provider factory (singleton - stateless)
        services.AddSingleton<ILlmProviderFactory, LlmProviderFactory>();

        // Register tracked provider factory (scoped - uses DbContext)
        services.AddScoped<ITrackedLlmProviderFactory, TrackedLlmProviderFactory>();

        // Register default ILlmProvider for backward compatibility
        services.AddScoped<ILlmProvider>(sp =>
        {
            var factory = sp.GetRequiredService<ILlmProviderFactory>();
            return factory.GetDefault();
        });

        return services;
    }

    private static string? GetSetting(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
