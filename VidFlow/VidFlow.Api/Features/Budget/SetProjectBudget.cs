using VidFlow.Api.Data;

namespace VidFlow.Api.Features.Budget;

public static class SetProjectBudget
{
    public record Request(decimal BudgetCapUsd);

    public record Response(
        Guid ProjectId,
        decimal BudgetCapUsd,
        decimal CurrentSpendUsd,
        decimal RemainingBudget);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/projects/{projectId}/budget", Handler)
           .WithName("SetProjectBudget")
           .WithTags("Budget");
    }

    private static async Task<IResult> Handler(
        Guid projectId,
        Request request,
        VidFlowDbContext db,
        CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return Results.NotFound($"Project {projectId} not found");

        try
        {
            project.SetBudgetCap(request.BudgetCapUsd);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new Response(
                project.Id,
                project.BudgetCapUsd,
                project.CurrentSpendUsd,
                project.GetRemainingBudget()));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}
