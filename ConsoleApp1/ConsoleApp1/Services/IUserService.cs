using ConsoleApp1.Common;
using ConsoleApp1.Models;
using ConsoleApp1.Models.Dto;

namespace ConsoleApp1.Services;

public interface IUserService
{
    OperationResult Register(RegisterRequest request);
    OperationResult<User> Authorize(LoginRequest request);
    OperationResult EditUser(EditUserRequest request);
    OperationResult DeleteUser(int id);
    List<User> GetAllUsers();
    string ExportUsersAsJson(DateTime? from, DateTime? to);
}
