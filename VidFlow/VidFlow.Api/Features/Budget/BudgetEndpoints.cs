namespace VidFlow.Api.Features.Budget;

public static class BudgetEndpoints
{
    public static void MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        SetProjectBudget.MapEndpoint(app);
        GetProjectCosts.MapEndpoint(app);
    }
}
