using System.Security.Cryptography;

namespace ConsoleApp1.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public string GenerateSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSize));
    }

    public string Hash(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSize);
        return Convert.ToBase64String(hashBytes);
    }

    public bool Verify(string password, string salt, string expectedHash)
    {
        var actualHash = Hash(password, salt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(actualHash),
            Convert.FromBase64String(expectedHash));
    }
}
