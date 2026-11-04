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

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/invitations/{invitationId:int}/publish")]
    public async Task<IActionResult> Publish(int invitationId, CancellationToken cancellationToken)
    {
        try { await invitations.PublishAsync(invitationId, cancellationToken); return NoContent(); }
        catch (InvitationNotFoundException exception) { return NotFoundProblem(exception.Message); }
        catch (InvitationValidationException exception) { return ValidationProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/invitations/{invitationId:int}/pause")]
    public async Task<IActionResult> Pause(int invitationId, CancellationToken cancellationToken)
    {
        try { await invitations.PauseAsync(invitationId, cancellationToken); return NoContent(); }
        catch (InvitationNotFoundException exception) { return NotFoundProblem(exception.Message); }
        catch (InvitationValidationException exception) { return ValidationProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/invitations/{invitationId:int}/reactivate")]
    public async Task<IActionResult> Reactivate(int invitationId, CancellationToken cancellationToken)
    {
        try { await invitations.ReactivateAsync(invitationId, cancellationToken); return NoContent(); }
        catch (InvitationNotFoundException exception) { return NotFoundProblem(exception.Message); }
        catch (InvitationValidationException exception) { return ValidationProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/invitations/{invitationId:int}/extend")]
    public async Task<IActionResult> Extend(int invitationId, CancellationToken cancellationToken)
    {
        try { await invitations.ExtendAsync(invitationId, cancellationToken); return NoContent(); }
        catch (InvitationNotFoundException exception) { return NotFoundProblem(exception.Message); }
        catch (InvitationValidationException exception) { return ValidationProblem(exception.Message); }
    }

    [AllowAnonymous]
    [HttpGet("public/invitations/{slug}")]
    [EnableRateLimiting("public-invitation")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> GetPublic(string slug, CancellationToken cancellationToken)
    {
        try { return Ok(PublicInvitationResponse.FromData(await invitations.GetPublicAsync(slug, cancellationToken), slug)); }
        catch (InvitationNotFoundException exception) { return NotFoundProblem(exception.Message); }
    }

    [AllowAnonymous]
    [HttpGet("public/invitations/{slug}/media/{mediaId:int}")]
    [EnableRateLimiting("public-invitation")]
    public async Task<IActionResult> GetPublicMedia(string slug, int mediaId, CancellationToken cancellationToken)
    {
        var media = await invitations.GetPublicMediaAsync(slug, mediaId, cancellationToken);
        if (media is null) return NotFound();
        var stream = await storage.OpenReadAsync(media.StorageKey, cancellationToken);
        return stream is null ? NotFound() : File(stream, media.ContentType);
    }

    [AllowAnonymous]
    [HttpGet("public/invitations/{slug}/final-card")]
    [EnableRateLimiting("public-invitation")]
    public async Task<IActionResult> GetPublicFinalCard(string slug, CancellationToken cancellationToken)
    {
        var card = await invitations.GetPublicFinalPngAsync(slug, cancellationToken);
        if (card is null) return NotFound();
        var stream = await storage.OpenReadAsync(card.StorageKey, cancellationToken);
        return stream is null ? NotFound() : File(stream, card.ContentType, "wedlink-invitation.png");
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/invitations/{invitationId:int}/final-card")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> SaveFinalCard(int invitationId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length is < 24 or > 20 * 1024 * 1024)
            return ValidationProblem("ملف البطاقة يجب أن يكون PNG ولا يتجاوز 20MB.");
        await using var input = file.OpenReadStream();
        var header = new byte[24];
        var read = await input.ReadAsync(header, cancellationToken);
        if (read != header.Length || !header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            || !header.AsSpan(12, 4).SequenceEqual("IHDR"u8)
            || System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4)) is < 1 or > 10000
            || System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4)) is < 1 or > 10000)
            return ValidationProblem("تعذر التحقق من صورة PNG.");
        input.Position = 0;
        var key = await storage.SaveAsync(input, cancellationToken);
        try
        {
            await invitations.SetFinalPngAsync(invitationId, key, cancellationToken);
            return NoContent();
        }
        catch (InvitationNotFoundException exception) { await storage.DeleteAsync(key, cancellationToken); return NotFoundProblem(exception.Message); }
        catch (InvitationValidationException exception) { await storage.DeleteAsync(key, cancellationToken); return ValidationProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/invitations/{invitationId:int}/final-card")]
    public async Task<IActionResult> GetFinalCard(int invitationId, CancellationToken cancellationToken)
    {
        var card = await invitations.GetFinalPngAsync(invitationId, cancellationToken);
        if (card is null) return NotFound();
        var stream = await storage.OpenReadAsync(card.StorageKey, cancellationToken);
        return stream is null ? NotFound() : File(stream, card.ContentType, "wedlink-invitation.png");
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
