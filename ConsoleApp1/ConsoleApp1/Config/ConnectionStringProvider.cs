using System.Text.Json;

namespace ConsoleApp1.Config;

public static class ConnectionStringProvider
{
    public static string Get(string name = "Postgres")
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var json = File.ReadAllText(path);

        using var document = JsonDocument.Parse(json);
        var value = document.RootElement.GetProperty("ConnectionStrings").GetProperty(name).GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Строка подключения \"{name}\" не найдена в appsettings.json");
        }

        return value;
    }
}
