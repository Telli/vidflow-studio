namespace VidFlow.Api.Features.Projects;

/// <summary>
/// Extension methods for mapping all project-related endpoints.
/// </summary>
public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        CreateProject.MapEndpoint(app);
        GetProject.MapEndpoint(app);
        UpdateProject.MapEndpoint(app);
        ListProjects.MapEndpoint(app);
        
        return app;
    }
}
