using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WedLink.API.Contracts.AdminAccounts;
using WedLink.Business.AdminAccounts;

namespace WedLink.API.Controllers;

[ApiController]
[Authorize(Roles = AdminRoles.SuperAdmin)]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController(AdminAccountService adminAccountService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminAccountResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var accounts = await adminAccountService.GetAllAsync(cancellationToken);
        return Ok(accounts.Select(AdminAccountResponse.FromData).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateAdminAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var account = await adminAccountService.CreateAsync(
                new CreateAdminAccountInput(request.Email, request.Password, request.Roles),
                cancellationToken);
            return CreatedAtAction(nameof(GetAll), AdminAccountResponse.FromData(account));
        }
        catch (AdminAccountValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPatch("{userId}/status")]
    public async Task<IActionResult> SetStatus(
        string userId,
        AdminAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var account = await adminAccountService.SetActiveAsync(
                userId, request.IsActive, actorId, cancellationToken);
            return Ok(AdminAccountResponse.FromData(account));
        }
        catch (AdminAccountNotFoundException)
        {
            return NotFound();
        }
        catch (AdminAccountValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{userId}/roles")]
    public async Task<IActionResult> SetRoles(
        string userId,
        AdminAccountRolesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var account = await adminAccountService.SetRolesAsync(
                userId, request.Roles, actorId, cancellationToken);
            return Ok(AdminAccountResponse.FromData(account));
        }
        catch (AdminAccountNotFoundException)
        {
            return NotFound();
        }
        catch (AdminAccountValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
