using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using WedLink.DataAccess.Audit;
using WedLink.DataAccess.Persistence;

namespace WedLink.API.Filters;

public sealed class AdminAuditFilter(IServiceScopeFactory scopes, TimeProvider clock, ILogger<AdminAuditFilter> logger) : IAsyncActionFilter
{
    private static readonly HashSet<string> IdKeys = ["orderId", "paymentId", "invitationId", "customerId", "userId", "templateId", "templateVersionId", "messageId", "musicTrackId", "mediaId", "linkId", "addOnId"];
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext; var method = http.Request.Method;
        if (!http.Request.Path.StartsWithSegments("/api/v1/admin") || method is "GET" or "HEAD" or "OPTIONS" || http.User.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value)) { await next(); return; }
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var entityId = context.RouteData.Values.LastOrDefault(x => IdKeys.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).Value?.ToString();
        ActionExecutedContext executed;
        try { executed = await next(); }
        catch
        {
            await PersistAsync(http, descriptor, entityId, StatusCodes.Status500InternalServerError);
            throw;
        }
        var status = (executed.Result as IStatusCodeActionResult)?.StatusCode ?? http.Response.StatusCode;
        entityId ??= TryGetCreatedId(executed.Result);
        await PersistAsync(http, descriptor, entityId, status);
    }

    private async Task PersistAsync(HttpContext http, ControllerActionDescriptor? descriptor, string? entityId, int status)
    {
        var log = new AuditLog { AdminUserId = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,
            Action = $"{descriptor?.ControllerName ?? "Admin"}.{descriptor?.ActionName ?? "Write"}", EntityType = descriptor?.ControllerName ?? "Admin", EntityId = entityId,
            Outcome = status < 400 ? "Succeeded" : "Failed", HttpStatusCode = status, CreatedAt = clock.GetUtcNow() };
        try { await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<WedLinkDbContext>(); db.AuditLogs.Add(log); await db.SaveChangesAsync(http.RequestAborted); }
        catch (Exception ex) { logger.LogError(ex, "Could not persist an admin audit event."); }
    }

    private static string? TryGetCreatedId(IActionResult? result)
    {
        var value = (result as ObjectResult)?.Value;
        if (value is null) return null;
        foreach (var name in new[] { "Id", "OrderId", "CustomerId", "InvitationId", "TemplateId", "MusicTrackId" })
        {
            var id = value.GetType().GetProperty(name)?.GetValue(value)?.ToString();
            if (!string.IsNullOrWhiteSpace(id)) return id;
        }
        return null;
    }
}
