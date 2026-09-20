using ConsoleApp1.Models;
using Npgsql;

namespace ConsoleApp1.Repositories;

public class PostgresUserRepository : IUserRepository
{
    private readonly string _connectionString;

    public PostgresUserRepository(string connectionString)
    {
        _connectionString = connectionString;
        EnsureSchema();
    }

    public List<User> GetAll()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, username, password_hash, salt, created_at, updated_at FROM users ORDER BY id";

        var users = new List<User>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            users.Add(Map(reader));
        }

        return users;
    }

    public User? GetById(int id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, username, password_hash, salt, created_at, updated_at FROM users WHERE id = @id";
        command.Parameters.AddWithValue("id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public User? GetByUsername(string username)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, username, password_hash, salt, created_at, updated_at FROM users WHERE lower(username) = lower(@username)";
        command.Parameters.AddWithValue("username", username);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public void Add(User user)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO users (username, password_hash, salt, created_at, updated_at)
            VALUES (@username, @passwordHash, @salt, @createdAt, @updatedAt)
            RETURNING id
            """;
        command.Parameters.AddWithValue("username", user.Username);
        command.Parameters.AddWithValue("passwordHash", user.PasswordHash);
        command.Parameters.AddWithValue("salt", user.Salt);
        command.Parameters.AddWithValue("createdAt", user.CreatedAt);
        command.Parameters.AddWithValue("updatedAt", user.UpdatedAt);

        try
        {
            user.Id = (int)command.ExecuteScalar()!;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateUsernameException();
        }
    }

    public void Update(User user)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE users
            SET username = @username, password_hash = @passwordHash, salt = @salt, updated_at = @updatedAt
            WHERE id = @id
            """;
        command.Parameters.AddWithValue("username", user.Username);
        command.Parameters.AddWithValue("passwordHash", user.PasswordHash);
        command.Parameters.AddWithValue("salt", user.Salt);
        command.Parameters.AddWithValue("updatedAt", user.UpdatedAt);
        command.Parameters.AddWithValue("id", user.Id);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateUsernameException();
        }
    }

    public void Delete(int id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM users WHERE id = @id";
        command.Parameters.AddWithValue("id", id);
        command.ExecuteNonQuery();
    }

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT pg_advisory_xact_lock(286408841);
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                username TEXT NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                salt TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_users_username_lower ON users (lower(username));
            """;
        command.ExecuteNonQuery();

        command.CommandText = """
            SELECT atttypid = 'timestamp without time zone'::regtype
            FROM pg_attribute WHERE attrelid = 'users'::regclass AND attname = 'created_at'
            """;
        if (command.ExecuteScalar() is true)
        {
            MigrateLegacyDates(command);
        }

        transaction.Commit();
    }

    private static void MigrateLegacyDates(NpgsqlCommand command)
    {
        var legacyTimeZone = Environment.GetEnvironmentVariable("LEGACY_DB_TIME_ZONE");
        if (string.IsNullOrWhiteSpace(legacyTimeZone))
        {
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM users)";
            if (command.ExecuteScalar() is true)
            {
                throw new InvalidOperationException("Укажите LEGACY_DB_TIME_ZONE для преобразования старых дат");
            }

            legacyTimeZone = "UTC";
        }

        command.CommandText = "SELECT EXISTS (SELECT 1 FROM pg_timezone_names WHERE name = @zone)";
        command.Parameters.AddWithValue("zone", legacyTimeZone);
        if (command.ExecuteScalar() is false)
        {
            throw new InvalidOperationException("LEGACY_DB_TIME_ZONE должен содержать имя часового пояса PostgreSQL");
        }

        // Применяем исходный пояс только в транзакции миграции.
        command.CommandText = "SELECT set_config('TimeZone', @zone, true)";
        command.ExecuteScalar();
        command.Parameters.Clear();
        command.CommandText = """
            ALTER TABLE users
                ALTER COLUMN created_at TYPE TIMESTAMPTZ,
                ALTER COLUMN updated_at TYPE TIMESTAMPTZ
            """;
        command.ExecuteNonQuery();
    }

    private NpgsqlConnection OpenConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static User Map(NpgsqlDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            Salt = reader.GetString(3),
            CreatedAt = reader.GetDateTime(4),
            UpdatedAt = reader.GetDateTime(5)
        };
    }
}
