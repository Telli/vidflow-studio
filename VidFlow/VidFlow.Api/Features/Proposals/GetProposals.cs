using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;
using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Features.Proposals;

public static class GetProposals
{
    public record Response(
        List<ProposalGroupDto> ProposalGroups);

    public record ProposalGroupDto(
        AgentRole Role,
        List<ProposalDto> Proposals);

    public record ProposalDto(
        Guid Id,
        AgentRole Role,
        string Summary,
        string Rationale,
        int RuntimeImpactSeconds,
        string Diff,
        ProposalStatus Status,
        DateTime CreatedAt,
        int TokensUsed,
        decimal CostUsd);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/scenes/{sceneId}/proposals", Handler)
           .WithName("GetProposals")
           .WithTags("Proposals");
    }

    private static async Task<IResult> Handler(
        Guid sceneId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var scene = await db.Scenes
            .Include(s => s.Proposals)
            .FirstOrDefaultAsync(s => s.Id == sceneId, ct);
        
        if (scene is null)
            return Results.NotFound($"Scene with ID {sceneId} not found.");

        // Group proposals by agent role
        var proposalGroups = scene.Proposals
            .GroupBy(p => p.Role)
            .Select(g => new ProposalGroupDto(
                g.Key,
                g.OrderByDescending(p => p.CreatedAt)
                 .Select(p => new ProposalDto(
                     p.Id,
                     p.Role,
                     p.Summary,
                     p.Rationale,
                     p.RuntimeImpactSeconds,
                     p.Diff,
                     p.Status,
                     p.CreatedAt,
                     p.TokensUsed,
                     p.CostUsd))
                 .ToList()))
            .OrderBy(g => g.Role)
            .ToList();

        var response = new Response(proposalGroups);
        return Results.Ok(response);
    }
}
