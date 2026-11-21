using System.Globalization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/reports/users", (ReportUser?[] users, ILoggerFactory loggerFactory) =>
{
    var report = new StringBuilder("ID | Username | Created (UTC) | Updated (UTC)").AppendLine();
    foreach (var user in users)
    {
        if (user is null || user.Id <= 0 || string.IsNullOrWhiteSpace(user.Username)
            || user.Username.Contains('\r') || user.Username.Contains('\n'))
        {
            return Results.BadRequest();
        }

        report.AppendLine(CultureInfo.InvariantCulture,
            $"{user.Id} | {user.Username} | {user.CreatedAt.UtcDateTime:O} | {user.UpdatedAt.UtcDateTime:O}");
    }

    var logger = loggerFactory.CreateLogger("UserReport");
    logger.LogInformation("Отчёт сформирован, строк: {UserCount}", users.Length);
    return Results.Text(report.ToString(), "text/plain", Encoding.UTF8);
});

app.Run();

record ReportUser(int Id, string Username, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed class ReportServiceAssembly;
