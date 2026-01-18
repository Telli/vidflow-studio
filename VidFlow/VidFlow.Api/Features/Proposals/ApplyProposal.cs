using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.Exceptions;

namespace VidFlow.Api.Features.Proposals;

public static class ApplyProposal
{
    public record Response(
        Guid Id,
        AgentRole Role,
        string Summary,
        ProposalStatus Status,
        DateTime AppliedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/proposals/{proposalId}/apply", Handler)
           .WithName("ApplyProposal")
           .WithTags("Proposals");
    }

    private static async Task<IResult> Handler(
        Guid proposalId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var proposal = await db.AgentProposals
            .Include(p => p.Scene)
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);
        
        if (proposal is null)
            return Results.NotFound($"Proposal with ID {proposalId} not found.");

        if (proposal.Status != ProposalStatus.Pending)
            return Results.BadRequest($"Proposal {proposalId} is not in Pending status.");

        var scene = proposal.Scene;

        if (scene.IsCurrentlyLocked())
            throw new ConcurrentModificationException(scene.Id);
        
        // Check if scene is editable
        if (scene.Status != SceneStatus.Draft)
            throw new SceneNotEditableException(scene.Id, scene.Status);

        // Apply the proposal diff to the scene
        ApplyProposalDiff(scene, proposal.Diff);

        // Mark proposal as applied
        proposal.Apply();

        // ApplyProposalDiff updates the scene and bumps version once.
        foreach (var evt in scene.DomainEvents)
        {
            db.EventStore.Add(EventStoreEntry.FromDomainEvent(evt, scene.ProjectId, scene.Id));
        }
        scene.ClearDomainEvents();

        await db.SaveChangesAsync(ct);

        var response = new Response(
            proposal.Id,
            proposal.Role,
            proposal.Summary,
            proposal.Status,
            DateTime.UtcNow);

        return Results.Ok(response);
    }

    private static void ApplyProposalDiff(Scene scene, string diff)
    {
        if (string.IsNullOrWhiteSpace(diff))
            return;

        try
        {
            var diffData = JsonSerializer.Deserialize<ProposalDiff>(diff);
            if (diffData == null)
                return;

            // Apply all changes in one update to avoid inflating version/event count.
            if (diffData.Title == null &&
                diffData.Script == null &&
                diffData.NarrativeGoal == null &&
                diffData.EmotionalBeat == null &&
                diffData.Location == null &&
                diffData.TimeOfDay == null &&
                diffData.CharacterNames == null)
            {
                return;
            }

            scene.Update(
                title: diffData.Title,
                narrativeGoal: diffData.NarrativeGoal,
                emotionalBeat: diffData.EmotionalBeat,
                location: diffData.Location,
                timeOfDay: diffData.TimeOfDay,
                script: diffData.Script,
                characterNames: diffData.CharacterNames);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid proposal diff format: {ex.Message}", ex);
        }
    }

    private record ProposalDiff(
        string? Title = null,
        string? Script = null,
        string? NarrativeGoal = null,
        string? EmotionalBeat = null,
        string? Location = null,
        string? TimeOfDay = null,
        List<string>? CharacterNames = null);
}
