using Hangfire;
using Hangfire.Storage;

namespace VidFlow.Api.Features.Jobs;

public static class GetJobStatus
{
    public record Response(
        string JobId,
        string State,
        DateTime? CreatedAt,
        DateTime? LastStateChangedAt,
        string? Reason);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/jobs/{jobId}", Handler)
           .WithName("GetJobStatus")
           .WithTags("Jobs");
    }

    private static IResult Handler(string jobId, JobStorage jobStorage)
    {
        var api = jobStorage.GetMonitoringApi();
        var details = api.JobDetails(jobId);

        if (details is null)
            return Results.NotFound($"Job {jobId} not found");

        var history = details.History ?? [];
        var createdAt = history.Count > 0 ? history.Min(h => h.CreatedAt) : (DateTime?)null;
        var latest = history.Count > 0 ? history.MaxBy(h => h.CreatedAt) : null;

        var state = latest?.StateName ?? "Unknown";

        string? reason = null;
        if (latest?.Data is not null)
        {
            if (latest.Data.TryGetValue("ExceptionMessage", out var exceptionMessage) && !string.IsNullOrWhiteSpace(exceptionMessage))
                reason = exceptionMessage;
            else if (latest.Data.TryGetValue("Reason", out var stateReason) && !string.IsNullOrWhiteSpace(stateReason))
                reason = stateReason;
        }

        return Results.Ok(new Response(
            jobId,
            state,
            createdAt,
            latest?.CreatedAt,
            reason));
    }
}
