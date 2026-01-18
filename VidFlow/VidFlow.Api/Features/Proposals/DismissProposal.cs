using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;
using VidFlow.Api.Domain.Exceptions;

namespace VidFlow.Api.Features.Proposals;

public static class DismissProposal
{
    public record Response(
        Guid Id,
        AgentRole Role,
        string Summary,
        ProposalStatus Status,
        DateTime DismissedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/proposals/{proposalId}/dismiss", Handler)
           .WithName("DismissProposal")
           .WithTags("Proposals");
    }

    private static async Task<IResult> Handler(
        Guid proposalId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var proposal = await db.AgentProposals
            .FirstOrDefaultAsync(p => p.Id == proposalId, ct);
        
        if (proposal is null)
            return Results.NotFound($"Proposal with ID {proposalId} not found.");

        if (proposal.Status != ProposalStatus.Pending)
            return Results.BadRequest($"Proposal {proposalId} is not in Pending status.");

        var scene = await db.Scenes.FindAsync([proposal.SceneId], ct);
        if (scene is null)
            return Results.NotFound($"Scene with ID {proposal.SceneId} not found.");

        if (scene.IsCurrentlyLocked())
            throw new ConcurrentModificationException(scene.Id);

        if (scene.Status != SceneStatus.Draft)
            throw new SceneNotEditableException(scene.Id, scene.Status);

        // Mark proposal as dismissed
        proposal.Dismiss();

        await db.SaveChangesAsync(ct);

        var response = new Response(
            proposal.Id,
            proposal.Role,
            proposal.Summary,
            proposal.Status,
            DateTime.UtcNow);

        return Results.Ok(response);
    }
}
