using ConsoleApp1.Common;
using ConsoleApp1.Models;
using ConsoleApp1.Models.Dto;
using ConsoleApp1.Services;

namespace ConsoleApp1.Controllers;

public class UserController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    public OperationResult Register(string username, string password)
    {
        return _userService.Register(new RegisterRequest(username, password));
    }

    public OperationResult<User> Authorize(string username, string password)
    {
        return _userService.Authorize(new LoginRequest(username, password));
    }

    public OperationResult EditUser(int id, string? username, string? password)
    {
        return _userService.EditUser(new EditUserRequest(id, username, password));
    }

    public OperationResult DeleteUser(int id)
    {
        return _userService.DeleteUser(id);
    }

    public List<User> GetAllUsers()
    {
        return _userService.GetAllUsers();
    }

    public string ExportUsersAsJson(DateTime? from, DateTime? to)
    {
        return _userService.ExportUsersAsJson(from, to);
    }
}
