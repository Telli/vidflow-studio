namespace VidFlow.Api.Features.Notifications;

public static class TestWebhook
{
    public record Request(string WebhookUrl);

    public record Response(bool Success, string Message);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/webhooks/test", Handler)
           .WithName("TestWebhook")
           .WithTags("Notifications");
    }

    private static async Task<IResult> Handler(
        Request request,
        IWebhookService webhookService,
        CancellationToken ct)
    {
        var testPayload = new WebhookPayload(
            "TestWebhook",
            Guid.Empty,
            null,
            new { Message = "This is a test webhook from VidFlow Studio" },
            DateTime.UtcNow);

        var result = await webhookService.SendWebhookAsync(request.WebhookUrl, testPayload, ct);

        if (!result.Success)
        {
            return Results.BadRequest(new Response(false, result.ErrorMessage ?? "Webhook rejected or failed."));
        }

        return Results.Ok(new Response(true, $"Test webhook sent to {request.WebhookUrl}"));
    }
}
