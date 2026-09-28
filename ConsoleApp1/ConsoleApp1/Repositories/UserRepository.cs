using System.Text.Json;
using ConsoleApp1.Models;

namespace ConsoleApp1.Data;

public class UserRepository
{
    private readonly string _filePath;
    private readonly List<User> _users;

    public UserRepository(string filePath = "users.json")
    {
        _filePath = filePath;
        _users = Load();
    }

    public List<User> GetAll()
    {
        return _users;
    }

    public User? GetById(int id)
    {
        return _users.FirstOrDefault(u => u.Id == id);
    }

    public User? GetByUsername(string username)
    {
        return _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public void Add(User user)
    {
        user.Id = _users.Count == 0 ? 1 : _users.Max(u => u.Id) + 1;
        _users.Add(user);
        Save();
    }

    public void Update(User user)
    {
        var index = _users.FindIndex(u => u.Id == user.Id);
        if (index == -1)
        {
            return;
        }

        _users[index] = user;
        Save();
    }

    public void Delete(int id)
    {
        _users.RemoveAll(u => u.Id == id);
        Save();
    }

    private List<User> Load()
    {
        if (!File.Exists(_filePath))
        {
            return new List<User>();
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
