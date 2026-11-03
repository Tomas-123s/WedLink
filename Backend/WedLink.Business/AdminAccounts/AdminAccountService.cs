namespace WedLink.Business.AdminAccounts;

public sealed class AdminAccountService(IAdminAccountRepository repository)
{
    public Task<IReadOnlyList<AdminAccountData>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return repository.GetAllAsync(cancellationToken);
    }

    public async Task<AdminAccountData> CreateAsync(
        CreateAdminAccountInput input,
        CancellationToken cancellationToken = default)
    {
        var email = input.Email.Trim();
        if (email.Length == 0 || email.Length > 256)
        {
            throw new AdminAccountValidationException("Enter a valid email address.");
        }

        var roles = CleanRoles(input.Roles);
        var result = await repository.CreateAsync(
            email, input.Password, roles, cancellationToken);
        return GetResult(result);
    }

    public async Task<AdminAccountData> SetActiveAsync(
        string userId,
        bool isActive,
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AdminAccountNotFoundException();

        if (!isActive && userId == actingUserId)
        {
            throw new AdminAccountValidationException("You cannot deactivate your own account.");
        }

        if (!isActive
            && existing.IsActive
            && existing.Roles.Contains(AdminRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase)
            && await repository.CountActiveSuperAdminsAsync(cancellationToken) <= 1)
        {
            throw new AdminAccountValidationException("At least one active SuperAdmin is required.");
        }

        var result = await repository.SetActiveAsync(userId, isActive, cancellationToken)
            ?? throw new AdminAccountNotFoundException();
        return GetResult(result);
    }

    public async Task<AdminAccountData> SetRolesAsync(
        string userId,
        IReadOnlyList<string> requestedRoles,
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        var roles = CleanRoles(requestedRoles);
        var existing = await repository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AdminAccountNotFoundException();

        if (userId == actingUserId && !roles.Contains(AdminRoles.SuperAdmin))
        {
            throw new AdminAccountValidationException("You cannot remove your own SuperAdmin role.");
        }

        if (existing.IsActive
            && existing.Roles.Contains(AdminRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase)
            && !roles.Contains(AdminRoles.SuperAdmin)
            && await repository.CountActiveSuperAdminsAsync(cancellationToken) <= 1)
        {
            throw new AdminAccountValidationException("At least one active SuperAdmin is required.");
        }

        var result = await repository.SetRolesAsync(userId, roles, cancellationToken)
            ?? throw new AdminAccountNotFoundException();
        return GetResult(result);
    }

    private static IReadOnlyList<string> CleanRoles(IReadOnlyList<string>? requestedRoles)
    {
        if (requestedRoles is null || requestedRoles.Count == 0)
        {
            throw new AdminAccountValidationException("Assign at least one supported role.");
        }

        var roles = requestedRoles
            .Select(role => role.Trim())
            .Where(role => role.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roles.Count == 0 || roles.Any(role => !AdminRoles.ManagedRoles.Contains(role)))
        {
            throw new AdminAccountValidationException("One or more roles are not supported.");
        }

        return roles;
    }

    private static AdminAccountData GetResult(AdminAccountResult result)
    {
        if (result.Succeeded && result.Account is not null)
        {
            return result.Account;
        }

        throw new AdminAccountValidationException(
            string.Join(" ", result.Errors));
    }
}
