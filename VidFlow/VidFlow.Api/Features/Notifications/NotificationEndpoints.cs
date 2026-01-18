namespace VidFlow.Api.Features.Notifications;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        // SSRF-sensitive endpoint: only expose during local development.
        if (app.Environment.IsDevelopment())
        {
            TestWebhook.MapEndpoint(app);
        }
    }
}
