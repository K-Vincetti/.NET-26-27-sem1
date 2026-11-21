using System.Text.Json;
using ConsoleApp1.Models;
using ConsoleApp1.Models.Dto;
using ConsoleApp1.Repositories;
using ConsoleApp1.Services;
using ConsoleApp1.Validation;

namespace ConsoleApp1.Tests;

public class UserServiceTests
{
    private readonly MemoryUserRepository _repository = new();
    private readonly PasswordHasher _hasher = new();
    private UserService Service => new(_repository, _hasher, new UserValidator());

    [Fact]
    public void RegisterStoresSaltedHashAndUtcDates()
    {
        Assert.True(Service.Register(new("first", "Example123")).Success);
        Assert.True(Service.Register(new("second", "Example123")).Success);

        var users = _repository.GetAll();
        Assert.All(users, user =>
        {
            Assert.NotEqual("Example123", user.PasswordHash);
            Assert.True(_hasher.Verify("Example123", user.Salt, user.PasswordHash));
            Assert.Equal(DateTimeKind.Utc, user.CreatedAt.Kind);
            Assert.Equal(user.CreatedAt, user.UpdatedAt);
        });
        Assert.NotEqual(users[0].Salt, users[1].Salt);
        Assert.NotEqual(users[0].PasswordHash, users[1].PasswordHash);
    }

    [Theory]
    [InlineData("bad user", "Example123")]
    [InlineData("valid_name", "abc")]
    [InlineData("", "Example123")]
    [InlineData("valid_name\n", "Example123")]
    public void RegisterRejectsInvalidInput(string username, string password)
    {
        Assert.False(Service.Register(new(username, password)).Success);
        Assert.Empty(_repository.GetAll());
    }

    [Fact]
    public void DuplicateLoginIsRejectedRegardlessOfCase()
    {
        Assert.True(Service.Register(new("first", "Example123")).Success);
        Assert.False(Service.Register(new("FIRST", "Example123")).Success);
        Assert.Single(_repository.GetAll());
    }

    [Fact]
    public void AuthorizeReturnsPublicFieldsForValidPassword()
    {
        Assert.True(Service.Register(new("first", "Example123")).Success);

        var result = Service.Authorize(new("FIRST", "Example123"));

        Assert.True(result.Success);
        Assert.Equal(new AuthenticatedUserDto(1, "first"), result.Data);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Data));
        Assert.Equal(new[] { "Id", "Username" },
            json.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
    }

    [Theory]
    [InlineData("first", "Wrong123")]
    [InlineData("missing", "Example123")]
    [InlineData("first", "")]
    public void AuthorizeRejectsInvalidCredentials(string username, string password)
    {
        Assert.True(Service.Register(new("first", "Example123")).Success);
        var result = Service.Authorize(new(username, password));
        Assert.False(result.Success);
        Assert.Null(result.Data);
    }

    [Fact]
    public void EditChangesCredentialsAndPreservesCreationDate()
    {
        Assert.True(Service.Register(new("first", "Example123")).Success);
        var user = Assert.Single(_repository.GetAll());
        var createdAt = user.CreatedAt;
        var initialSalt = user.Salt;

        Assert.True(Service.EditUser(new(user.Id, "updated", "Replacement456")).Success);

        Assert.Equal(createdAt, user.CreatedAt);
        Assert.True(user.UpdatedAt >= createdAt);
        Assert.NotEqual(initialSalt, user.Salt);
        Assert.False(Service.Authorize(new("first", "Example123")).Success);
        Assert.False(Service.Authorize(new("updated", "Example123")).Success);
        Assert.True(Service.Authorize(new("updated", "Replacement456")).Success);
    }

    [Fact]
    public void EditRejectsEmptyChangesAndTakenLogin()
    {
        Assert.True(Service.Register(new("first", "Example123")).Success);
        Assert.True(Service.Register(new("second", "Example123")).Success);

        Assert.False(Service.EditUser(new(1, null, null)).Success);
        Assert.False(Service.EditUser(new(1, "SECOND", null)).Success);
        Assert.Equal("first", _repository.GetById(1)!.Username);
    }

    [Fact]
    public void DeleteRemovesUserAndPreventsLogin()
    {
        Assert.True(Service.Register(new("first", "Example123")).Success);
        Assert.True(Service.DeleteUser(1).Success);
        Assert.False(Service.DeleteUser(1).Success);
        Assert.False(Service.Authorize(new("first", "Example123")).Success);
        Assert.Empty(_repository.GetAll());
    }

    [Theory]
    [InlineData(UserDateFilter.Created, new[] { "created" })]
    [InlineData(UserDateFilter.Updated, new[] { "updated" })]
    [InlineData(UserDateFilter.CreatedOrUpdated, new[] { "created", "updated" })]
    public void ExportFiltersBySelectedDateAndIncludesExactBoundaries(UserDateFilter filter, string[] expected)
    {
        var day = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
        var end = day.AddDays(1).AddTicks(-1);
        _repository.Add(new User { Username = "created", CreatedAt = day, UpdatedAt = end.AddTicks(1) });
        _repository.Add(new User { Username = "updated", CreatedAt = day.AddTicks(-1), UpdatedAt = end });
        _repository.Add(new User { Username = "outside", CreatedAt = end.AddTicks(1), UpdatedAt = end.AddTicks(1) });

        var result = Service.ExportUsersAsJson(day, end, filter);

        Assert.True(result.Success);
        var users = JsonSerializer.Deserialize<UserExportDto[]>(result.Data!)!;
        Assert.Equal(expected, users.Select(user => user.Username).ToArray());
    }

    [Fact]
    public void ExportWithoutBoundsReturnsOnlyPublicFieldsAndReversedRangeFails()
    {
        Assert.True(Service.Register(new("first", "Example123")).Success);

        var now = DateTime.UtcNow;
        Assert.False(Service.ExportUsersAsJson(now, now.AddDays(-1)).Success);

        var result = Service.ExportUsersAsJson(null, null);
        Assert.True(result.Success);
        using var json = JsonDocument.Parse(result.Data!);
        var user = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal(new[] { "Id", "Username", "CreatedAt", "UpdatedAt" },
            user.EnumerateObject().Select(property => property.Name).ToArray());
    }

    private sealed class MemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<int, User> _users = new();
        private int _nextId = 1;

        public List<User> GetAll() => _users.Values.ToList();
        public User? GetById(int id) => _users.GetValueOrDefault(id);
        public User? GetByUsername(string username) => _users.Values
            .SingleOrDefault(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));

        public void Add(User user)
        {
            user.Id = _nextId++;
            _users.Add(user.Id, user);
        }

        public void Update(User user) => _users[user.Id] = user;
        public void Delete(int id) => _users.Remove(id);
    }
}
