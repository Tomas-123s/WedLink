namespace WedLink.Business.AdminAccounts;

public static class AdminRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";

    public static IReadOnlySet<string> ManagedRoles { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            SuperAdmin,
            Admin
        };
}

public sealed record AdminAccountData(
    string Id,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles);

public sealed record CreateAdminAccountInput(
    string Email,
    string Password,
    IReadOnlyList<string> Roles);

public sealed record AdminAccountResult(
    bool Succeeded,
    AdminAccountData? Account,
    IReadOnlyList<string> Errors);

public sealed class AdminAccountValidationException(string message) : Exception(message);

public sealed class AdminAccountNotFoundException() : Exception("Admin account was not found.");

public interface IAdminAccountRepository
{
    Task<IReadOnlyList<AdminAccountData>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AdminAccountData?> GetByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveSuperAdminsAsync(CancellationToken cancellationToken = default);

    Task<AdminAccountResult> CreateAsync(
        string email,
        string password,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default);

    Task<AdminAccountResult?> SetActiveAsync(
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<AdminAccountResult?> SetRolesAsync(
        string userId,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default);
}
