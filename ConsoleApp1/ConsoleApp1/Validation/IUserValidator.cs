namespace ConsoleApp1.Validation;

public interface IUserValidator
{
    List<string> ValidateUsername(string? username);
    List<string> ValidatePassword(string? password);
}
