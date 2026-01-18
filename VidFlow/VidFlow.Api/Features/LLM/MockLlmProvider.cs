namespace VidFlow.Api.Features.LLM;

public class MockLlmProvider : ILlmProvider
{
    public string ProviderName { get; }

    public MockLlmProvider(string providerName)
    {
        ProviderName = providerName;
    }

    public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct)
    {
        var content = request.Prompt;

        return Task.FromResult(new LlmResponse(
            content,
            0,
            0m,
            request.Model,
            DateTime.UtcNow));
    }
}
