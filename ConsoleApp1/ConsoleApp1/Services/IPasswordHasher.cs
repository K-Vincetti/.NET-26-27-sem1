namespace ConsoleApp1.Services;

public interface IPasswordHasher
{
    string GenerateSalt();
    string Hash(string password, string salt);
    bool Verify(string password, string salt, string expectedHash);
}
