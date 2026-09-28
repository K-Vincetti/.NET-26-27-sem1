using ConsoleApp1.Config;
using ConsoleApp1.Controllers;
using ConsoleApp1.Repositories;
using ConsoleApp1.Services;
using ConsoleApp1.Validation;

IUserRepository repository = new PostgresUserRepository(ConnectionStringProvider.Get());
IPasswordHasher hasher = new PasswordHasher();
IUserValidator validator = new UserValidator();
IUserService userService = new UserService(repository, hasher, validator);
var controller = new UserController(userService);

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
        case "0":
            return;
        default:
            Console.WriteLine("Некорректный пункт меню");
            break;
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

static void ExportUsers(UserController controller)
{
    var from = ReadOptionalDate("Дата начала (например 2026-09-01), Enter — без ограничения: ");
    var to = ReadOptionalDate("Дата конца (например 2026-09-30), Enter — без ограничения: ");
    var toInclusive = to?.Date.AddDays(1).AddTicks(-1);

    var json = controller.ExportUsersAsJson(from, toInclusive);
    Console.WriteLine(json);
}

static DateTime? ReadOptionalDate(string prompt)
{
    Console.Write(prompt);
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        return null;
    }

    if (DateTime.TryParse(input, out var date))
    {
        return date;
    }

    Console.WriteLine("Не удалось распознать дату, значение проигнорировано");
    return null;
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
