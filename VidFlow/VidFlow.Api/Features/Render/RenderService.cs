using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Hangfire;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.Events;
using VidFlow.Api.Hubs;
using VidFlow.Api.Features.Jobs;

namespace VidFlow.Api.Features.Render;

/// <summary>
/// Service for managing video rendering jobs.
/// In production, this would integrate with actual video rendering infrastructure (FFmpeg, cloud render farm, etc.)
/// Currently implements mock rendering with simulated progress for demonstration.
/// </summary>
public class RenderService
{
    private readonly VidFlowDbContext _db;
    private readonly IHubContext<AgentActivityHub> _hub;
    private readonly IHostEnvironment _env;
    private readonly IBackgroundJobClient _backgroundJobs;

    public RenderService(VidFlowDbContext db, IHubContext<AgentActivityHub> hub, IHostEnvironment env, IBackgroundJobClient backgroundJobs)
    {
        _db = db;
        _hub = hub;
        _env = env;
        _backgroundJobs = backgroundJobs;
    }

    public async Task<RenderJob> RequestAnimaticAsync(Guid sceneId, CancellationToken ct)
    {
        var scene = await _db.Scenes.FindAsync([sceneId], ct);
        if (scene is null)
            throw new InvalidOperationException($"Scene {sceneId} not found");

        var job = RenderJob.CreateAnimatic(scene.ProjectId, sceneId, scene.Version);
        _db.RenderJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        // Start async rendering via durable job queue
        _backgroundJobs.Enqueue<RenderJobProcessor>(p => p.RunAsync(job.Id));

        return job;
    }

    public async Task<RenderJob> RequestSceneRenderAsync(Guid sceneId, CancellationToken ct)
    {
        var scene = await _db.Scenes
            .Include(s => s.Shots)
            .FirstOrDefaultAsync(s => s.Id == sceneId, ct);

        if (scene is null)
            throw new InvalidOperationException($"Scene {sceneId} not found");

        if (scene.Status != SceneStatus.Approved)
            throw new InvalidOperationException($"Scene must be approved before rendering. Current status: {scene.Status}");

        var job = RenderJob.CreateSceneRender(scene.ProjectId, sceneId, scene.Version);
        _db.RenderJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        // Start async rendering via durable job queue
        _backgroundJobs.Enqueue<RenderJobProcessor>(p => p.RunAsync(job.Id));

        return job;
    }

    public async Task<RenderJob> RequestFinalRenderAsync(Guid projectId, CancellationToken ct)
    {
        var project = await _db.Projects.FindAsync([projectId], ct);
        if (project is null)
            throw new InvalidOperationException($"Project {projectId} not found");

        var stitchPlan = await _db.StitchPlans
            .FirstOrDefaultAsync(sp => sp.ProjectId == projectId, ct);

        if (stitchPlan is null || !stitchPlan.Entries.Any())
            throw new InvalidOperationException("Stitch plan is empty. Add approved scenes before final render.");

        // Verify all scenes in stitch plan are approved
        var sceneIds = stitchPlan.Entries.Select(e => e.SceneId).ToList();
        var scenes = await _db.Scenes
            .Where(s => sceneIds.Contains(s.Id))
            .ToListAsync(ct);

        var unapproved = scenes.Where(s => s.Status != SceneStatus.Approved).ToList();
        if (unapproved.Any())
            throw new InvalidOperationException($"All scenes must be approved. Unapproved: {string.Join(", ", unapproved.Select(s => s.Title))}");

        var job = RenderJob.CreateFinalRender(projectId);
        _db.RenderJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        // Start async rendering via durable job queue
        _backgroundJobs.Enqueue<RenderJobProcessor>(p => p.RunAsync(job.Id));

        return job;
    }

    public async Task<RenderJob?> GetRenderJobAsync(Guid jobId, CancellationToken ct)
    {
        return await _db.RenderJobs.FindAsync([jobId], ct);
    }

    public async Task<List<RenderJob>> GetRenderJobsForProjectAsync(Guid projectId, CancellationToken ct)
    {
        return await _db.RenderJobs
            .Where(j => j.ProjectId == projectId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task ProcessRenderJobAsync(Guid jobId, CancellationToken ct)
    {
        try
        {
            var job = await _db.RenderJobs.FindAsync([jobId], ct);
            if (job is null) return;

            job.Start();
            await _db.SaveChangesAsync(ct);
            await _hub.Clients.Group($"project-{job.ProjectId}").SendAsync("RenderStarted", jobId, job.Type.ToString(), ct);

            if (!FfmpegVideoRenderer.IsAvailable())
                throw new InvalidOperationException("ffmpeg not found on PATH. Install ffmpeg to enable real rendering.");

            var rendersRoot = Path.Combine(_env.ContentRootPath, "renders");
            Directory.CreateDirectory(rendersRoot);

            var (relativePath, label, durationSeconds, width, height) = job.Type switch
            {
                RenderType.Animatic => (Path.Combine("animatics", $"{job.SceneId}_{job.SourceVersion}.mp4"), $"Animatic {job.SceneId}", 2, 1280, 720),
                RenderType.Scene => (Path.Combine("scenes", $"{job.SceneId}_{job.SourceVersion}.mp4"), $"Scene {job.SceneId}", 2, 1280, 720),
                RenderType.Final => (Path.Combine("final", $"{job.ProjectId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mp4"), $"Final {job.ProjectId}", 4, 1280, 720),
                _ => (Path.Combine("unknown", $"{job.Id}.mp4"), $"Render {job.Id}", 2, 1280, 720)
            };

            var outputPath = Path.Combine(rendersRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? rendersRoot);

            job.UpdateProgress(10);
            await _db.SaveChangesAsync(ct);
            await _hub.Clients.Group($"project-{job.ProjectId}").SendAsync("RenderProgress", jobId, 10, ct);

            var renderer = new FfmpegVideoRenderer();
            await renderer.RenderPlaceholderMp4Async(outputPath, label, durationSeconds, width, height, ct);

            job.UpdateProgress(90);
            await _db.SaveChangesAsync(ct);
            await _hub.Clients.Group($"project-{job.ProjectId}").SendAsync("RenderProgress", jobId, 90, ct);

            var artifactPath = "/renders/" + relativePath.Replace("\\", "/");

            job.Complete(artifactPath);
            await _db.SaveChangesAsync(ct);

            // Emit domain event
            DomainEvent evt = job.Type == RenderType.Final
                ? new FinalRenderCompleted(job.ProjectId, job.Id)
                : new SceneRendered(job.SceneId!.Value, job.Id, job.SourceVersion ?? 1);

            _db.EventStore.Add(EventStoreEntry.FromDomainEvent(evt, job.ProjectId, job.SceneId ?? job.Id));
            await _db.SaveChangesAsync(ct);

            await _hub.Clients.Group($"project-{job.ProjectId}").SendAsync("RenderCompleted", jobId, artifactPath, ct);
        }
        catch (Exception ex)
        {
            // Handle failure
            try
            {
                var job = await _db.RenderJobs.FindAsync([jobId], ct);
                if (job is not null)
                {
                    job.Fail(ex.Message);
                    await _db.SaveChangesAsync(ct);
                    await _hub.Clients.Group($"project-{job.ProjectId}").SendAsync("RenderFailed", jobId, ex.Message, ct);
                }
            }
            catch { /* Ignore cleanup errors */ }

            throw;
        }
    }
}
