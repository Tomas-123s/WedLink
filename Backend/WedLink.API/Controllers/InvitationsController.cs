using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WedLink.API.Contracts.Invitations;
using WedLink.Business.CustomerSubmissions;
using WedLink.Business.Invitations;

namespace WedLink.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class InvitationsController(InvitationService invitations, ISubmissionFileStorage storage) : ControllerBase
{
    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/orders/{orderId:int}/invitation")]
    public async Task<IActionResult> GetForAdmin(int orderId, CancellationToken cancellationToken)
    {
        try { return Ok(InvitationResponse.FromData(await invitations.GetForAdminAsync(orderId, cancellationToken))); }
        catch (InvitationNotFoundException exception) { return NotFoundProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("admin/orders/{orderId:int}/invitation")]
    public async Task<IActionResult> Save(int orderId, SaveInvitationRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(InvitationResponse.FromData(await invitations.SaveAsync(orderId, request.ToInput(), cancellationToken))); }
        catch (InvitationNotFoundException exception) { return NotFoundProblem(exception.Message); }
        catch (InvitationValidationException exception) { return ValidationProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/orders/{orderId:int}/invitation/preview-links")]
    public async Task<IActionResult> CreatePreviewLink(int orderId, CancellationToken cancellationToken)
    {
        try
        {
            var link = await invitations.CreatePreviewLinkAsync(orderId, cancellationToken);
            return Ok(new CreatePreviewLinkResponse($"/preview#token={Uri.EscapeDataString(link.Token)}", link.ExpiresAt));
        }
        catch (InvitationNotFoundException exception) { return NotFoundProblem(exception.Message); }
    }

    [AllowAnonymous]
    [HttpGet("invitation-previews/current")]
    [EnableRateLimiting("private-preview")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> GetPreview(CancellationToken cancellationToken)
    {
        try { return Ok(PreviewInvitationResponse.FromData(await invitations.GetPreviewAsync(GetToken(), cancellationToken))); }
        catch (PreviewLinkException exception) { return UnauthorizedProblem(exception.Message); }
    }

    [AllowAnonymous]
    [HttpGet("invitation-previews/current/media/{mediaId:int}")]
    [EnableRateLimiting("private-preview")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> GetPreviewMedia(int mediaId, CancellationToken cancellationToken)
    {
        try
        {
            var media = await invitations.GetPreviewMediaAsync(GetToken(), mediaId, cancellationToken);
            if (media is null) return NotFound();
            var stream = await storage.OpenReadAsync(media.StorageKey, cancellationToken);
            return stream is null ? NotFound() : File(stream, media.ContentType);
        }
        catch (PreviewLinkException exception) { return UnauthorizedProblem(exception.Message); }
    }

    [AllowAnonymous]
    [HttpPost("invitation-previews/current/review")]
    [EnableRateLimiting("private-preview")]
    public async Task<IActionResult> RecordReview(PreviewReviewRequest request, CancellationToken cancellationToken)
    {
        try { await invitations.RecordReviewAsync(GetToken(), request.Action, request.Comment, cancellationToken); return NoContent(); }
        catch (PreviewLinkException exception) { return UnauthorizedProblem(exception.Message); }
        catch (InvitationValidationException exception) { return ValidationProblem(exception.Message); }
    }

    private string GetToken()
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : string.Empty;
    }

    private ObjectResult UnauthorizedProblem(string detail) => Problem(statusCode: StatusCodes.Status401Unauthorized, title: "رابط المعاينة غير صالح", detail: detail);
    private ObjectResult NotFoundProblem(string detail) => Problem(statusCode: StatusCodes.Status404NotFound, title: "الدعوة أو الطلب غير موجود", detail: detail);
    private ObjectResult ValidationProblem(string detail) => Problem(statusCode: StatusCodes.Status400BadRequest, title: "بيانات غير صالحة", detail: detail);
}
