using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WedLink.DataAccess.Identity;
using WedLink.DataAccess.Persistence;

const string userSecretsId = "69aba4ee-3e87-47b9-9599-446399e25a90";
const string superAdminRole = "SuperAdmin";

var configuration = new ConfigurationBuilder()
    .AddUserSecrets(userSecretsId)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("WedLinkDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Connection string 'WedLinkDb' is missing.");
    Console.Error.WriteLine("Configure it in WedLink.API User Secrets first.");
    return 1;
}

Console.Write("Admin email: ");
var email = Console.ReadLine()?.Trim();

if (string.IsNullOrWhiteSpace(email))
{
    Console.Error.WriteLine("Email is required.");
    return 1;
}

Console.Write("Admin display name: ");
var displayName = Console.ReadLine()?.Trim();

if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 100)
{
    Console.Error.WriteLine("Display name is required (up to 100 characters).");
    return 1;
}

Console.Write("Password (8+ chars, upper/lowercase, digit, symbol): ");
var password = ReadPassword();
Console.WriteLine();

var services = new ServiceCollection();
services.AddLogging();
services.AddDbContext<WedLinkDbContext>(options => options.UseSqlServer(connectionString));
services
    .AddIdentityCore<AdminUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<WedLinkDbContext>();

await using var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<WedLinkDbContext>();
await dbContext.Database.MigrateAsync();

if (await dbContext.Users.AnyAsync())
{
    Console.Error.WriteLine("An admin account already exists. Initial setup can only run once.");
    return 1;
}

var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
if (!await roleManager.RoleExistsAsync(superAdminRole))
{
    var roleResult = await roleManager.CreateAsync(new IdentityRole(superAdminRole));
    if (!roleResult.Succeeded)
    {
        PrintErrors(roleResult.Errors);
        return 1;
    }
}

var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
if (await userManager.FindByEmailAsync(email) is not null)
{
    Console.Error.WriteLine("An admin with this email already exists.");
    return 1;
}

var user = new AdminUser
{
    DisplayName = displayName,
    UserName = email,
    Email = email,
    EmailConfirmed = true,
    LockoutEnabled = true
};

var createResult = await userManager.CreateAsync(user, password);
if (!createResult.Succeeded)
{
    PrintErrors(createResult.Errors);
    return 1;
}

var assignRoleResult = await userManager.AddToRoleAsync(user, superAdminRole);
if (!assignRoleResult.Succeeded)
{
    PrintErrors(assignRoleResult.Errors);
    return 1;
}

Console.WriteLine($"Admin account created: {email}");
return 0;

static string ReadPassword()
{
    var password = new List<char>();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            break;
        }

        if (key.Key == ConsoleKey.Backspace && password.Count > 0)
        {
            password.RemoveAt(password.Count - 1);
            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            password.Add(key.KeyChar);
        }
    }

    return new string(password.ToArray());
}

static void PrintErrors(IEnumerable<IdentityError> errors)
{
    foreach (var error in errors)
    {
        Console.Error.WriteLine(error.Description);
    }
}
