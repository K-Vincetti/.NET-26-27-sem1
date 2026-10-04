using ConsoleApp1.Common;
using ConsoleApp1.Models;
using ConsoleApp1.Models.Dto;
using ConsoleApp1.Services;
using Microsoft.Extensions.Logging;

namespace ConsoleApp1.Controllers;

public class UserController
{
    private readonly IUserService _userService;

    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public OperationResult Register(string username, string password)
    {
        return Execute(nameof(Register), () => _userService.Register(new RegisterRequest(username, password)));
    }

    public OperationResult<AuthenticatedUserDto> Authorize(string username, string password)
    {
        return Execute(nameof(Authorize), () => _userService.Authorize(new LoginRequest(username, password)));
    }

    public OperationResult EditUser(int id, string? username, string? password)
    {
        return Execute(nameof(EditUser), () => _userService.EditUser(new EditUserRequest(id, username, password)));
    }

    public OperationResult DeleteUser(int id)
    {
        return Execute(nameof(DeleteUser), () => _userService.DeleteUser(id));
    }

    public List<User> GetAllUsers()
    {
        return Execute(nameof(GetAllUsers), () => _userService.GetAllUsers());
    }

    public OperationResult<string> ExportUsersAsJson(DateTime? from, DateTime? to,
        UserDateFilter filter = UserDateFilter.CreatedOrUpdated)
    {
        return Execute(nameof(ExportUsersAsJson), () => _userService.ExportUsersAsJson(from, to, filter));
    }

    private T Execute<T>(string operation, Func<T> action)
    {
        try
        {
            var result = action();
            if (result is OperationResult { Success: false })
            {
                _logger.LogWarning("Операция {Operation} отклонена", operation);
            }
            else
            {
                _logger.LogInformation("Операция {Operation} выполнена", operation);
            }

            return result;
        }
        catch (Exception exception)
        {
            _logger.LogError("Сбой операции {Operation} ({ExceptionType})", operation, exception.GetType().Name);
            throw;
        }
    }
}
