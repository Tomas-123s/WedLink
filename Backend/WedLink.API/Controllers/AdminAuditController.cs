using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WedLink.Business.Audit;

namespace WedLink.API.Controllers;

[ApiController, Route("api/v1/admin/audit-logs"), Authorize(Roles = "SuperAdmin")]
public sealed class AdminAuditController(AuditLogService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? userId, [FromQuery] string? action, [FromQuery] string? entityType, [FromQuery] string? entityId,
        [FromQuery] string? outcome, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        try { return Ok(await audit.SearchAsync(userId, action, entityType, entityId, outcome, from, to, page, pageSize, cancellationToken)); }
        catch (AuditValidationException e) { return BadRequest(new ProblemDetails { Title = "Invalid filters", Detail = e.Message, Status = 400 }); }
    }
}
