using System.Text.RegularExpressions;

namespace ConsoleApp1.Validation;

public class UserValidator : IUserValidator
{
    private const int UsernameMinLength = 3;
    private const int UsernameMaxLength = 30;
    private const int PasswordMinLength = 6;
    private const int PasswordMaxLength = 100;

    private static readonly Regex UsernamePattern = new("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    public List<string> ValidateUsername(string? username)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(username))
        {
            errors.Add("Логин не может быть пустым");
            return errors;
        }

        if (username.Length < UsernameMinLength || username.Length > UsernameMaxLength)
        {
            errors.Add($"Логин должен быть от {UsernameMinLength} до {UsernameMaxLength} символов");
        }

        if (!UsernamePattern.IsMatch(username))
        {
            errors.Add("Логин может содержать только латинские буквы, цифры и подчёркивание");
        }

        return errors;
    }

    public List<string> ValidatePassword(string? password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Пароль не может быть пустым");
            return errors;
        }

        if (password.Length < PasswordMinLength || password.Length > PasswordMaxLength)
        {
            errors.Add($"Пароль должен быть от {PasswordMinLength} до {PasswordMaxLength} символов");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Пароль должен содержать хотя бы одну цифру");
        }

        if (!password.Any(char.IsLetter))
        {
            errors.Add("Пароль должен содержать хотя бы одну букву");
        }

        if (password.Any(char.IsWhiteSpace))
        {
            errors.Add("Пароль не должен содержать пробелов");
        }

        return errors;
    }
}
