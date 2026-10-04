using System.Globalization;
using ConsoleApp1.Models.Dto;
using ConsoleApp1.Config;
using ConsoleApp1.Controllers;
using ConsoleApp1.Repositories;
using ConsoleApp1.Services;
using ConsoleApp1.Validation;
using Microsoft.Extensions.Logging;
using Npgsql;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true)
    .AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace));
var logger = loggerFactory.CreateLogger("ConsoleApp1");
IUserRepository repository;
try
{
    repository = new PostgresUserRepository(ConnectionStringProvider.Get());
}
catch (Exception exception)
{
    logger.LogError("Не удалось запустить приложение ({ExceptionType})", exception.GetType().Name);
    Console.Error.WriteLine("Проверьте подключение к PostgreSQL и настройки схемы в README.");
    Environment.ExitCode = 1;
    return;
}
IPasswordHasher hasher = new PasswordHasher();
IUserValidator validator = new UserValidator();
IUserService userService = new UserService(repository, hasher, validator);
var controller = new UserController(userService, loggerFactory.CreateLogger<UserController>());

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1 - Регистрация");
    Console.WriteLine("2 - Авторизация");
    Console.WriteLine("3 - Редактировать пользователя");
    Console.WriteLine("4 - Удалить пользователя");
    Console.WriteLine("5 - Список пользователей");
    Console.WriteLine("6 - Экспорт пользователей в JSON за период");
    Console.WriteLine("0 - Выход");
    Console.Write("Выберите пункт: ");

    var choice = Console.ReadLine();
    Console.WriteLine();

    try
    {
        switch (choice)
        {
            case "1":
                RegisterUser(controller);
                break;
            case "2":
                LoginUser(controller);
                break;
            case "3":
                EditUser(controller);
                break;
            case "4":
                DeleteUser(controller);
                break;
            case "5":
                ShowUsers(controller);
                break;
            case "6":
                ExportUsers(controller);
                break;
            case null:
            case "0":
                return;
            default:
                Console.WriteLine("Некорректный пункт меню");
                break;
        }
    }
    catch (NpgsqlException)
    {
        Console.Error.WriteLine("Операция не выполнена. Проверьте доступность PostgreSQL.");
    }
}

static void RegisterUser(UserController controller)
{
    Console.Write("Логин: ");
    var username = Console.ReadLine() ?? string.Empty;
    Console.Write("Пароль: ");
    var password = ReadPassword();

    var result = controller.Register(username, password);
    Console.WriteLine(result.Message);
}

static void LoginUser(UserController controller)
{
    Console.Write("Логин: ");
    var username = Console.ReadLine() ?? string.Empty;
    Console.Write("Пароль: ");
    var password = ReadPassword();

    var result = controller.Authorize(username, password);
    Console.WriteLine(result.Message);
    if (result.Success && result.Data != null)
    {
        Console.WriteLine($"Добро пожаловать, {result.Data.Username} (id = {result.Data.Id})");
    }
}

static void EditUser(UserController controller)
{
    Console.Write("Id пользователя: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Некорректный id");
        return;
    }

    Console.Write("Новый логин (Enter — оставить прежним): ");
    var username = Console.ReadLine();

    Console.Write("Новый пароль (Enter — оставить прежним): ");
    var password = ReadPassword();

    var result = controller.EditUser(id, username, string.IsNullOrEmpty(password) ? null : password);
    Console.WriteLine(result.Message);
}

static void DeleteUser(UserController controller)
{
    Console.Write("Id пользователя: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Некорректный id");
        return;
    }

    var result = controller.DeleteUser(id);
    Console.WriteLine(result.Message);
}

static void ShowUsers(UserController controller)
{
    var users = controller.GetAllUsers();
    if (users.Count == 0)
    {
        Console.WriteLine("Пользователей пока нет");
        return;
    }

    foreach (var user in users)
    {
        Console.WriteLine($"{user.Id} | {user.Username} | создан {user.CreatedAt:g} | обновлён {user.UpdatedAt:g}");
    }
}

void ExportUsers(UserController controller)
{
    Console.Write("По какой дате (1 — создание, 2 — обновление, Enter — любая): ");
    var selection = Console.ReadLine();
    if (selection is not ("" or "1" or "2"))
    {
        logger.LogWarning("Экспорт отклонён: Некорректный вариант фильтра");
        Console.WriteLine("Некорректный вариант фильтра");
        return;
    }

    var filter = selection switch
    {
        "1" => UserDateFilter.Created,
        "2" => UserDateFilter.Updated,
        _ => UserDateFilter.CreatedOrUpdated
    };
    if (!TryReadOptionalDate("Дата начала UTC (ГГГГ-ММ-ДД), Enter — без ограничения: ", TimeOnly.MinValue, out var from)
        || !TryReadOptionalDate("Дата конца UTC (ГГГГ-ММ-ДД), Enter — без ограничения: ", TimeOnly.MaxValue, out var to))
    {
        logger.LogWarning("Экспорт отклонён: Некорректная дата. Используйте формат ГГГГ-ММ-ДД");
        Console.WriteLine("Некорректная дата. Используйте формат ГГГГ-ММ-ДД");
        return;
    }

    var result = controller.ExportUsersAsJson(from, to, filter);
    Console.WriteLine(result.Success ? result.Data : result.Message);
}

static bool TryReadOptionalDate(string prompt, TimeOnly time, out DateTime? value)
{
    Console.Write(prompt);
    var input = Console.ReadLine();
    value = null;
    if (input == "")
    {
        return true;
    }

    if (!DateOnly.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
    {
        return false;
    }

    value = date.ToDateTime(time, DateTimeKind.Utc);
    return true;
}

static string ReadPassword()
{
    if (Console.IsInputRedirected)
    {
        return Console.ReadLine() ?? string.Empty;
    }

    var password = string.Empty;
    ConsoleKeyInfo key;
    do
    {
        key = Console.ReadKey(true);

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
            {
                password = password[..^1];
                Console.Write("\b \b");
            }
            continue;
        }

        if (key.Key != ConsoleKey.Enter)
        {
            password += key.KeyChar;
            Console.Write("*");
        }
    } while (key.Key != ConsoleKey.Enter);

    Console.WriteLine();
    return password;
}
