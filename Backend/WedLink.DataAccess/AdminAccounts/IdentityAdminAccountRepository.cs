using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WedLink.Business.AdminAccounts;
using WedLink.DataAccess.Identity;

namespace WedLink.DataAccess.AdminAccounts;

public sealed class IdentityAdminAccountRepository(
    UserManager<AdminUser> userManager,
    RoleManager<IdentityRole> roleManager) : IAdminAccountRepository
{
    public async Task<IReadOnlyList<AdminAccountData>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);
        var accounts = new List<AdminAccountData>(users.Count);

        foreach (var user in users)
        {
            accounts.Add(await ToDataAsync(user));
        }

        return accounts;
    }

    public async Task<AdminAccountData?> GetByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : await ToDataAsync(user);
    }

    public async Task<int> CountActiveSuperAdminsAsync(
        CancellationToken cancellationToken = default)
    {
        var superAdmins = await userManager.GetUsersInRoleAsync(AdminRoles.SuperAdmin);
        return superAdmins.Count(user => user.IsActive);
    }

    public async Task<AdminAccountResult> CreateAsync(
        string displayName,
        string email,
        string password,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default)
    {
        await EnsureRolesExistAsync(roles);
        var user = new AdminUser
        {
            DisplayName = displayName,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LockoutEnabled = true,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return Failure(createResult.Errors);
        }

        var roleResult = await userManager.AddToRolesAsync(user, roles);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Failure(roleResult.Errors);
        }

        return Success(await ToDataAsync(user));
    }

    public async Task<AdminAccountResult?> SetDisplayNameAsync(
        string userId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        user.DisplayName = displayName;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? Success(await ToDataAsync(user))
            : Failure(result.Errors);
    }

    public async Task<AdminAccountResult?> SetActiveAsync(
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        user.IsActive = isActive;
        if (isActive)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }

        var result = await userManager.UpdateSecurityStampAsync(user);
        return result.Succeeded
            ? Success(await ToDataAsync(user))
            : Failure(result.Errors);
    }

    public async Task<AdminAccountResult?> SetRolesAsync(
        string userId,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        await EnsureRolesExistAsync(roles);
        var existingRoles = await userManager.GetRolesAsync(user);
        var rolesToAdd = roles
            .Except(existingRoles, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var rolesToRemove = existingRoles
            .Except(roles, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rolesToAdd.Count > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return Failure(addResult.Errors);
            }
        }

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                if (rolesToAdd.Count > 0)
                {
                    await userManager.RemoveFromRolesAsync(user, rolesToAdd);
                }

                return Failure(removeResult.Errors);
            }
        }

        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        return stampResult.Succeeded
            ? Success(await ToDataAsync(user))
            : Failure(stampResult.Errors);
    }

    private async Task EnsureRolesExistAsync(IReadOnlyList<string> roles)
    {
        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded && !await roleManager.RoleExistsAsync(roleName))
            {
                throw new InvalidOperationException("Could not provision a managed admin role.");
            }
        }
    }

    private async Task<AdminAccountData> ToDataAsync(AdminUser user)
    {
        return new AdminAccountData(
            user.Id,
            user.DisplayName,
            user.Email ?? user.UserName ?? string.Empty,
            user.IsActive,
            (await userManager.GetRolesAsync(user)).ToList());
    }

    private static AdminAccountResult Success(AdminAccountData account) =>
        new(true, account, []);

    private static AdminAccountResult Failure(IEnumerable<IdentityError> errors) =>
        new(false, null, errors.Select(error => error.Description).ToList());
}


