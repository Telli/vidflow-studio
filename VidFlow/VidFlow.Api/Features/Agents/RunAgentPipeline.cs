using Microsoft.EntityFrameworkCore;
using Hangfire;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Features.Jobs;

namespace VidFlow.Api.Features.Agents;

public static class RunAgentPipeline
{
    public record Response(
        string JobId,
        Guid SceneId);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/scenes/{sceneId}/run-agents", Handler)
           .WithName("RunAgentPipeline")
           .WithTags("Agents");
    }

    private static async Task<IResult> Handler(
        Guid sceneId,
        VidFlowDbContext db,
        IBackgroundJobClient backgroundJobs,
        CancellationToken ct)
    {
        var scene = await db.Scenes.FindAsync([sceneId], ct);
        if (scene is null)
            return Results.NotFound($"Scene with ID {sceneId} not found.");

        if (scene.Status != SceneStatus.Draft)
            return Results.BadRequest($"Scene must be in Draft status to run agent pipeline.");

        // PerformContext is automatically injected by Hangfire at runtime
        var jobId = backgroundJobs.Enqueue<AgentPipelineJob>(j => j.RunAsync(sceneId, null));
        var response = new Response(jobId, sceneId);

        return Results.Accepted($"/api/jobs/{jobId}", response);
    }
}
