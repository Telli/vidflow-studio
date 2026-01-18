using System.Security.Claims;

namespace VidFlow.Api.Features.Auth;

public static class Me
{
    public record Response(Guid Id, string Email);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", Handler)
           .WithName("Me")
           .WithTags("Auth");
    }

    private static IResult Handler(ClaimsPrincipal user)
    {
        var idRaw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub") ?? "";
        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email") ?? user.Identity?.Name ?? "";

        if (!Guid.TryParse(idRaw, out var id) || string.IsNullOrWhiteSpace(email))
            return Results.Unauthorized();

        return Results.Ok(new Response(id, email));
    }
}
