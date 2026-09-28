using System.Text.Json;
using ConsoleApp1.Common;
using ConsoleApp1.Models;
using ConsoleApp1.Models.Dto;
using ConsoleApp1.Repositories;
using ConsoleApp1.Validation;

namespace ConsoleApp1.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _hasher;
    private readonly IUserValidator _validator;

    public UserService(IUserRepository repository, IPasswordHasher hasher, IUserValidator validator)
    {
        _repository = repository;
        _hasher = hasher;
        _validator = validator;
    }

    public OperationResult Register(RegisterRequest request)
    {
        var errors = new List<string>();
        errors.AddRange(_validator.ValidateUsername(request.Username));
        errors.AddRange(_validator.ValidatePassword(request.Password));

        if (errors.Count > 0)
        {
            return OperationResult.Fail(string.Join("; ", errors));
        }

        if (_repository.GetByUsername(request.Username) != null)
        {
            return OperationResult.Fail("Пользователь с таким логином уже существует");
        }

        var salt = _hasher.GenerateSalt();
        var now = DateTime.Now;
        var user = new User
        {
            Username = request.Username,
            Salt = salt,
            PasswordHash = _hasher.Hash(request.Password, salt),
            CreatedAt = now,
            UpdatedAt = now
        };

        _repository.Add(user);
        return OperationResult.Ok($"Пользователь \"{user.Username}\" зарегистрирован, id = {user.Id}");
    }

    public OperationResult<User> Authorize(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return OperationResult<User>.Fail("Логин и пароль не могут быть пустыми");
        }

        var user = _repository.GetByUsername(request.Username);
        if (user == null)
        {
            return OperationResult<User>.Fail("Пользователь не найден");
        }

        if (!_hasher.Verify(request.Password, user.Salt, user.PasswordHash))
        {
            return OperationResult<User>.Fail("Неверный пароль");
        }

        return OperationResult<User>.Ok(user, "Авторизация успешна");
    }

    public OperationResult EditUser(EditUserRequest request)
    {
        var user = _repository.GetById(request.Id);
        if (user == null)
        {
            return OperationResult.Fail("Пользователь не найден");
        }

        var errors = new List<string>();
        var hasUsername = !string.IsNullOrWhiteSpace(request.Username);
        var hasPassword = !string.IsNullOrWhiteSpace(request.Password);

        if (hasUsername)
        {
            errors.AddRange(_validator.ValidateUsername(request.Username));

            var existing = _repository.GetByUsername(request.Username!);
            if (existing != null && existing.Id != request.Id)
            {
                errors.Add("Такой логин уже занят");
            }
        }

        if (hasPassword)
        {
            errors.AddRange(_validator.ValidatePassword(request.Password));
        }

        if (errors.Count > 0)
        {
            return OperationResult.Fail(string.Join("; ", errors));
        }

        if (!hasUsername && !hasPassword)
        {
            return OperationResult.Fail("Не указаны данные для изменения");
        }

        if (hasUsername)
        {
            user.Username = request.Username!;
        }

        if (hasPassword)
        {
            var salt = _hasher.GenerateSalt();
            user.Salt = salt;
            user.PasswordHash = _hasher.Hash(request.Password!, salt);
        }

        user.UpdatedAt = DateTime.Now;
        _repository.Update(user);
        return OperationResult.Ok("Данные пользователя обновлены");
    }

    public OperationResult DeleteUser(int id)
    {
        var user = _repository.GetById(id);
        if (user == null)
        {
            return OperationResult.Fail("Пользователь не найден");
        }

        _repository.Delete(id);
        return OperationResult.Ok($"Пользователь \"{user.Username}\" удалён");
    }

    public List<User> GetAllUsers()
    {
        return _repository.GetAll();
    }

    public string ExportUsersAsJson(DateTime? from, DateTime? to)
    {
        var users = _repository.GetAll()
            .Where(u => InRange(u.CreatedAt, from, to) || InRange(u.UpdatedAt, from, to))
            .OrderBy(u => u.Id)
            .Select(u => new UserExportDto(u.Id, u.Username, u.CreatedAt, u.UpdatedAt))
            .ToList();

        return JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
    }

    private static bool InRange(DateTime value, DateTime? from, DateTime? to)
    {
        if (from != null && value < from)
        {
            return false;
        }

        if (to != null && value > to)
        {
            return false;
        }

        return true;
    }
}
