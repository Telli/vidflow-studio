using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;

namespace VidFlow.Api.Features.Auth;

public static class Login
{
    public record Request(string Email, string Password);

    public record Response(Guid Id, string Email, string Token);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", Handler)
           .WithName("Login")
           .WithTags("Auth")
           .AllowAnonymous();
    }

    private static async Task<IResult> Handler(
        Request request,
        VidFlowDbContext db,
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
            return Results.Unauthorized();

        var ok = PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt);
        if (!ok)
            return Results.Unauthorized();

        var tokenService = serviceProvider.GetService<IJwtTokenService>();
        var token = tokenService?.CreateAccessToken(user) ?? "";
        return Results.Ok(new Response(user.Id, user.Email, token));
    }
}
