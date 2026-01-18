using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Enums;

namespace VidFlow.Api.Features.Budget;

public static class GetProjectCosts
{
    public record AgentCostBreakdown(
        string Role,
        int ProposalCount,
        int TotalTokens,
        decimal TotalCostUsd);

    public record SceneCostBreakdown(
        Guid SceneId,
        string SceneTitle,
        int ProposalCount,
        decimal TotalCostUsd);

    public record Response(
        Guid ProjectId,
        decimal BudgetCapUsd,
        decimal CurrentSpendUsd,
        decimal RemainingBudget,
        decimal BudgetUtilizationPercent,
        List<AgentCostBreakdown> CostsByAgent,
        List<SceneCostBreakdown> CostsByScene,
        int TotalProposals,
        int TotalTokensUsed);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId}/costs", Handler)
           .WithName("GetProjectCosts")
           .WithTags("Budget");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var project = await db.Projects
            .Include(p => p.Scenes)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        if (project is null)
            return Results.NotFound($"Project {projectId} not found");

        var proposals = await db.AgentProposals
            .Where(p => p.Scene!.ProjectId == projectId)
            .Include(p => p.Scene)
            .ToListAsync(ct);

        var costsByAgent = proposals
            .GroupBy(p => p.Role)
            .Select(g => new AgentCostBreakdown(
                g.Key.ToString(),
                g.Count(),
                g.Sum(p => p.TokensUsed),
                g.Sum(p => p.CostUsd)))
            .OrderByDescending(c => c.TotalCostUsd)
            .ToList();

        var costsByScene = proposals
            .GroupBy(p => new { p.SceneId, p.Scene!.Title })
            .Select(g => new SceneCostBreakdown(
                g.Key.SceneId,
                g.Key.Title,
                g.Count(),
                g.Sum(p => p.CostUsd)))
            .OrderByDescending(c => c.TotalCostUsd)
            .ToList();

        var totalCost = proposals.Sum(p => p.CostUsd);
        var utilizationPercent = project.BudgetCapUsd > 0 
            ? (totalCost / project.BudgetCapUsd) * 100 
            : 0;

        return Results.Ok(new Response(
            project.Id,
            project.BudgetCapUsd,
            totalCost,
            project.BudgetCapUsd > 0 ? project.BudgetCapUsd - totalCost : 0,
            utilizationPercent,
            costsByAgent,
            costsByScene,
            proposals.Count,
            proposals.Sum(p => p.TokensUsed)));
    }
}
