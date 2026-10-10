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
        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = request.Username,
            Salt = salt,
            PasswordHash = _hasher.Hash(request.Password, salt),
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            _repository.Add(user);
        }
        catch (DuplicateUsernameException)
        {
            return OperationResult.Fail("Пользователь с таким логином уже существует");
        }
        return OperationResult.Ok($"Пользователь \"{user.Username}\" зарегистрирован, id = {user.Id}");
    }

    public OperationResult<AuthenticatedUserDto> Authorize(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return OperationResult<AuthenticatedUserDto>.Fail("Логин и пароль не могут быть пустыми");
        }

        var user = _repository.GetByUsername(request.Username);
        if (user == null)
        {
            return OperationResult<AuthenticatedUserDto>.Fail("Пользователь не найден");
        }

        if (!_hasher.Verify(request.Password, user.Salt, user.PasswordHash))
        {
            return OperationResult<AuthenticatedUserDto>.Fail("Неверный пароль");
        }

        return OperationResult<AuthenticatedUserDto>.Ok(new AuthenticatedUserDto(user.Id, user.Username), "Авторизация успешна");
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

        user.UpdatedAt = DateTime.UtcNow;
        try
        {
            _repository.Update(user);
        }
        catch (DuplicateUsernameException)
        {
            return OperationResult.Fail("Такой логин уже занят");
        }
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

    public List<UserExportDto> GetAllUsers()
    {
        return _repository.GetAll().Select(ToExportDto).ToList();
    }

    public OperationResult<string> ExportUsersAsJson(DateTime? from, DateTime? to,
        UserDateFilter filter = UserDateFilter.CreatedOrUpdated)
    {
        from = NormalizeUtc(from);
        to = NormalizeUtc(to);
        if (from > to)
        {
            return OperationResult<string>.Fail("Дата начала позже даты конца");
        }

        var users = _repository.GetAll()
            .Where(user => filter switch
            {
                UserDateFilter.Created => InRange(user.CreatedAt, from, to),
                UserDateFilter.Updated => InRange(user.UpdatedAt, from, to),
                _ => InRange(user.CreatedAt, from, to) || InRange(user.UpdatedAt, from, to)
            })
            .OrderBy(u => u.Id)
            .Select(ToExportDto)
            .ToList();

        var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
        return OperationResult<string>.Ok(json, "Список пользователей получен");
    }

    private static UserExportDto ToExportDto(User user) =>
        new(user.Id, user.Username, user.CreatedAt, user.UpdatedAt);

    private static DateTime? NormalizeUtc(DateTime? value) => value?.Kind switch
    {
        DateTimeKind.Local => value.Value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        _ => value
    };

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
