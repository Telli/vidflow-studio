using Microsoft.EntityFrameworkCore;
using VidFlow.Api.Data;
using VidFlow.Api.Domain.Entities;

namespace VidFlow.Api.Features.Auth;

public static class Register
{
    public record Request(string Email, string Password);

    public record Response(Guid Id, string Email, string Token);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", Handler)
           .WithName("Register")
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
        if (string.IsNullOrWhiteSpace(email))
            return Results.BadRequest("Email is required");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return Results.BadRequest("Password must be at least 8 characters");

        var exists = await db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists)
            return Results.Conflict("Email already registered");

        var (hash, salt) = PasswordHasher.HashPassword(request.Password);
        var user = AppUser.Create(email, hash, salt);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var tokenService = serviceProvider.GetService<IJwtTokenService>();
        var token = tokenService?.CreateAccessToken(user);
        return Results.Ok(new Response(user.Id, user.Email, token ?? ""));
    }
}
