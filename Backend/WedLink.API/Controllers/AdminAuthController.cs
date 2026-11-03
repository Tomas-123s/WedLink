using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WedLink.DataAccess.Identity;

namespace WedLink.API.Controllers;

[ApiController]
[Route("api/v1/admin/auth")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly SignInManager<AdminUser> _signInManager;
    private readonly UserManager<AdminUser> _userManager;
    private readonly IAntiforgery _antiforgery;

    public AdminAuthController(
        SignInManager<AdminUser> signInManager,
        UserManager<AdminUser> userManager,
        IAntiforgery antiforgery)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _antiforgery = antiforgery;
    }

    [HttpGet("csrf")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var admin = await _userManager.FindByEmailAsync(request.Email);
        if (admin is null || !admin.IsActive)
        {
            return Unauthorized(new { message = "Invalid sign-in details." });
        }

        var result = await _signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "بيانات الدخول غير صحيحة." });
        }

        if (!await _userManager.IsInRoleAsync(admin, "SuperAdmin"))
        {
            await _signInManager.SignOutAsync();
            return Unauthorized(new { message = "Invalid sign-in details." });
        }

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult GetCurrentAdmin()
    {
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email)
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }
}

public sealed record LoginRequest(string Email, string Password);
