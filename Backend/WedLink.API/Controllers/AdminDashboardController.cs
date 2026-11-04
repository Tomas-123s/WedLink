using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WedLink.API.Contracts.Dashboard;
using WedLink.Business.Dashboard;

namespace WedLink.API.Controllers;

[ApiController, Route("api/v1/admin/dashboard"), Authorize(Roles = "SuperAdmin")]
public sealed class AdminDashboardController(AdminDashboardService dashboard) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> Summary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        try { return Ok(DashboardSummaryResponse.FromData(await dashboard.GetSummaryAsync(from, to, cancellationToken))); }
        catch (DashboardValidationException e) { return BadRequest(new ProblemDetails { Title = "Invalid date range", Detail = e.Message, Status = 400 }); }
    }
}
