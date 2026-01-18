using System.Security.Cryptography;

namespace VidFlow.Api.Features.Auth;

public static class PasswordHasher
{
    public static (string Hash, string Salt) HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password is required.", nameof(password));

        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public static bool Verify(string password, string expectedHashBase64, string saltBase64)
    {
        var saltBytes = Convert.FromBase64String(saltBase64);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        var actualHashBase64 = Convert.ToBase64String(hashBytes);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(actualHashBase64),
            Convert.FromBase64String(expectedHashBase64));
    }
}
