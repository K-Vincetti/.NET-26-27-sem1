using ConsoleApp1.Controllers;
using ConsoleApp1.Data;

var repository = new UserRepository();
var controller = new UserController(repository);

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1 - Регистрация");
    Console.WriteLine("2 - Авторизация");
    Console.WriteLine("3 - Редактировать пользователя");
    Console.WriteLine("4 - Удалить пользователя");
    Console.WriteLine("5 - Список пользователей");
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
    if (result.Success && result.User != null)
    {
        Console.WriteLine($"Добро пожаловать, {result.User.Username} (id = {result.User.Id})");
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
        Console.WriteLine($"{user.Id} | {user.Username} | зарегистрирован {user.CreatedAt:g}");
    }
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
