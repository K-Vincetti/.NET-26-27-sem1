using ConsoleApp1.Models;

namespace ConsoleApp1.Repositories;

public interface IUserRepository
{
    List<User> GetAll();
    User? GetById(int id);
    User? GetByUsername(string username);
    void Add(User user);
    void Update(User user);
    void Delete(int id);
}
