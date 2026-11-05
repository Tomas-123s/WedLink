using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using WedLink.API.Contracts.CustomerSubmissions;
using WedLink.Business.CustomerSubmissions;

namespace WedLink.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class CustomerSubmissionsController(
    CustomerSubmissionService submissionService,
    ISubmissionFileStorage storage) : ControllerBase
{
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/orders/{orderId:int}/submission-links")]
    public async Task<IActionResult> CreateLink(int orderId, CancellationToken cancellationToken)
    {
        try
        {
            var link = await submissionService.CreateLinkAsync(orderId, cancellationToken);
            var submissionUrl = $"/submit#token={Uri.EscapeDataString(link.Token)}";
            return Ok(new SubmissionLinkResponse(link.LinkId, submissionUrl, link.ExpiresAt));
        }
        catch (SubmissionNotFoundException exception) { return NotFoundProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/orders/{orderId:int}/submission-links/current")]
    public async Task<IActionResult> GetCurrentLink(int orderId, CancellationToken cancellationToken)
    {
        var link = await submissionService.GetCurrentLinkAsync(orderId, cancellationToken);
        return link is null ? NoContent() : Ok(link);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpDelete("admin/orders/{orderId:int}/submission-links/{linkId:int}")]
    public async Task<IActionResult> RevokeLink(int orderId, int linkId, CancellationToken cancellationToken)
    {
        try { await submissionService.RevokeLinkAsync(orderId, linkId, cancellationToken); return NoContent(); }
        catch (SubmissionNotFoundException exception) { return NotFoundProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/orders/{orderId:int}/customer-submission")]
    public async Task<IActionResult> GetForAdmin(int orderId, CancellationToken cancellationToken)
    {
        try { return Ok(WeddingSubmissionResponse.FromData(await submissionService.GetForAdminAsync(orderId, cancellationToken))); }
        catch (SubmissionNotFoundException exception) { return NotFoundProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/orders/{orderId:int}/customer-submission/media")]
    [RequestSizeLimit(110L * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAdminMedia(int orderId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null) return ValidationProblem("اختر صورة أو فيديو لرفعه.");
        try
        {
            await using var stream = file.OpenReadStream();
            var media = await submissionService.UploadForAdminAsync(orderId, file.FileName, file.ContentType,
                file.Length, stream, storage, cancellationToken);
            return Ok(SubmissionMediaResponse.FromData(media));
        }
        catch (SubmissionNotFoundException exception) { return NotFoundProblem(exception.Message); }
        catch (SubmissionValidationException exception) { return ValidationProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/orders/{orderId:int}/customer-submission/review")]
    public async Task<IActionResult> MarkReviewed(int orderId, CancellationToken cancellationToken)
    {
        try
        {
            await submissionService.MarkReviewedAsync(orderId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);
            return NoContent();
        }
        catch (SubmissionNotFoundException exception) { return NotFoundProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpDelete("admin/orders/{orderId:int}/customer-submission/media/{mediaId:int}")]
    public async Task<IActionResult> DeleteAdminMedia(int orderId, int mediaId, CancellationToken cancellationToken)
    {
        try
        {
            await submissionService.DeleteAdminMediaAsync(orderId, mediaId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, storage, cancellationToken);
            return NoContent();
        }
        catch (SubmissionNotFoundException exception) { return NotFoundProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/orders/{orderId:int}/customer-submission/media/{mediaId:int}/content")]
    public async Task<IActionResult> GetAdminMedia(int orderId, int mediaId, CancellationToken cancellationToken)
    {
        var item = await submissionService.GetMediaAsync(orderId, mediaId, cancellationToken);
        if (item is null || item.Value.Media is null || item.Value.StorageKey is null) return NotFound();
        var stream = await storage.OpenReadAsync(item.Value.StorageKey, cancellationToken);
        return stream is null ? NotFound() : File(stream, item.Value.Media.ContentType);
    }

    [AllowAnonymous]
    [HttpGet("customer-submissions/current")]
    [EnableRateLimiting("private-submission")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        try { return Ok(WeddingSubmissionResponse.FromData(await submissionService.GetCurrentAsync(GetToken(), cancellationToken))); }
        catch (SubmissionLinkException exception) { return UnauthorizedProblem(exception.Message); }
    }

    [AllowAnonymous]
    [HttpPut("customer-submissions/current")]
    [EnableRateLimiting("private-submission")]
    public async Task<IActionResult> SaveCurrent(SaveWeddingSubmissionRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(WeddingSubmissionResponse.FromData(await submissionService.SaveCurrentAsync(GetToken(), request.ToInput(), cancellationToken))); }
        catch (SubmissionLinkException exception) { return UnauthorizedProblem(exception.Message); }
        catch (SubmissionValidationException exception) { return ValidationProblem(exception.Message); }
    }

    [AllowAnonymous]
    [HttpPost("customer-submissions/current/media")]
    [RequestSizeLimit(110L * 1024 * 1024)]
    [EnableRateLimiting("private-upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCurrent([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var media = await submissionService.UploadAsync(GetToken(), file.FileName, file.ContentType,
                file.Length, stream, storage, cancellationToken);
            return Ok(SubmissionMediaResponse.FromData(media));
        }
        catch (SubmissionLinkException exception) { return UnauthorizedProblem(exception.Message); }
        catch (SubmissionValidationException exception) { return ValidationProblem(exception.Message); }
    }

    [AllowAnonymous]
    [HttpDelete("customer-submissions/current/media/{mediaId:int}")]
    [EnableRateLimiting("private-submission")]
    public async Task<IActionResult> DeleteCurrentMedia(int mediaId, CancellationToken cancellationToken)
    {
        try { await submissionService.DeleteCustomerMediaAsync(GetToken(), mediaId, storage, cancellationToken); return NoContent(); }
        catch (SubmissionLinkException exception) { return UnauthorizedProblem(exception.Message); }
        catch (SubmissionNotFoundException exception) { return NotFoundProblem(exception.Message); }
        catch (SubmissionValidationException exception) { return ValidationProblem(exception.Message); }
    }

    private string GetToken()
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : "";
    }

    private ObjectResult UnauthorizedProblem(string detail) => Problem(statusCode: StatusCodes.Status401Unauthorized, title: "رابط التقديم غير صالح", detail: detail);
    private ObjectResult NotFoundProblem(string detail) => Problem(statusCode: StatusCodes.Status404NotFound, title: "الطلب أو الملف غير موجود", detail: detail);
    private ObjectResult ValidationProblem(string detail) => Problem(statusCode: StatusCodes.Status400BadRequest, title: "بيانات غير صالحة", detail: detail);
}
