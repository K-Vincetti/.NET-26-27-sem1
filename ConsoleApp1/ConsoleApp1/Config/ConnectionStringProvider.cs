using System.Text.Json;
using Npgsql;

namespace ConsoleApp1.Config;

public static class ConnectionStringProvider
{
    public static string Get()
    {
        var configured = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var connection = new NpgsqlConnectionStringBuilder(
            document.RootElement.GetProperty("ConnectionStrings").GetProperty("Postgres").GetString());
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("Задайте POSTGRES_PASSWORD или ConnectionStrings__Postgres");
        }

        connection.Password = password;
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");
        if (!string.IsNullOrEmpty(port))
        {
            connection.Port = int.Parse(port);
        }

        return connection.ConnectionString;
    }
}
