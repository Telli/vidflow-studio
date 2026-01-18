namespace VidFlow.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        Register.MapEndpoint(app);
        Login.MapEndpoint(app);
        Me.MapEndpoint(app);
    }
}
