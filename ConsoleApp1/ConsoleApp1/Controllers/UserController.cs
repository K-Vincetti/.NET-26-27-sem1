using ConsoleApp1.Data;
using ConsoleApp1.Models;
using ConsoleApp1.Services;

namespace ConsoleApp1.Controllers;

public class UserController
{
    private readonly UserRepository _repository;

    public UserController(UserRepository repository)
    {
        _repository = repository;
    }

    public (bool Success, string Message) Register(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Логин и пароль не могут быть пустыми");
        }

        if (password.Length < 6)
        {
            return (false, "Пароль должен содержать минимум 6 символов");
        }

        if (_repository.GetByUsername(username) != null)
        {
            return (false, "Пользователь с таким логином уже существует");
        }

        var salt = PasswordHasher.GenerateSalt();
        var user = new User
        {
            Username = username,
            Salt = salt,
            PasswordHash = PasswordHasher.Hash(password, salt)
        };

        _repository.Add(user);
        return (true, $"Пользователь \"{username}\" зарегистрирован, id = {user.Id}");
    }

    public (bool Success, string Message, User? User) Authorize(string username, string password)
    {
        var user = _repository.GetByUsername(username);
        if (user == null)
        {
            return (false, "Пользователь не найден", null);
        }

        if (!PasswordHasher.Verify(password, user.Salt, user.PasswordHash))
        {
            return (false, "Неверный пароль", null);
        }

        return (true, "Авторизация успешна", user);
    }

    public (bool Success, string Message) EditUser(int id, string? newUsername, string? newPassword)
    {
        var user = _repository.GetById(id);
        if (user == null)
        {
            return (false, "Пользователь не найден");
        }

        if (!string.IsNullOrWhiteSpace(newUsername))
        {
            var existing = _repository.GetByUsername(newUsername);
            if (existing != null && existing.Id != id)
            {
                return (false, "Такой логин уже занят");
            }

            user.Username = newUsername;
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (newPassword.Length < 6)
            {
                return (false, "Пароль должен содержать минимум 6 символов");
            }

            var salt = PasswordHasher.GenerateSalt();
            user.Salt = salt;
            user.PasswordHash = PasswordHasher.Hash(newPassword, salt);
        }

        _repository.Update(user);
        return (true, "Данные пользователя обновлены");
    }

    public (bool Success, string Message) DeleteUser(int id)
    {
        var user = _repository.GetById(id);
        if (user == null)
        {
            return (false, "Пользователь не найден");
        }

        _repository.Delete(id);
        return (true, $"Пользователь \"{user.Username}\" удалён");
    }

    public List<User> GetAllUsers()
    {
        return _repository.GetAll();
    }
}
