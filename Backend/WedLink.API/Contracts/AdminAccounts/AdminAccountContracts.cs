using WedLink.Business.AdminAccounts;

namespace WedLink.API.Contracts.AdminAccounts;

public sealed record AdminAccountResponse(
    string Id,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles)
{
    public static AdminAccountResponse FromData(AdminAccountData account) =>
        new(account.Id, account.Email, account.IsActive, account.Roles);
}

public sealed record CreateAdminAccountRequest(
    string Email,
    string Password,
    IReadOnlyList<string> Roles);

public sealed record AdminAccountStatusRequest(bool IsActive);

public sealed record AdminAccountRolesRequest(IReadOnlyList<string> Roles);
