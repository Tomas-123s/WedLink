using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Encodings.Web;
using WedLink.API.Contracts.Invitations;
using WedLink.Business.CustomerSubmissions;
using WedLink.Business.Invitations;

namespace WedLink.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class InvitationsController(InvitationService invitations, ISubmissionFileStorage storage,
    IConfiguration configuration) : ControllerBase
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
    [HttpGet("public/invitations/{slug}/share")]
    [EnableRateLimiting("public-invitation")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> GetSharePage(string slug, CancellationToken cancellationToken)
    {
        PublicInvitationData invitation;
        try { invitation = await invitations.GetPublicAsync(slug, cancellationToken); }
        catch (InvitationNotFoundException) { return NotFound(); }

        var publicBaseUrl = configuration["PublicSite:BaseUrl"]?.TrimEnd('/');
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("https" or "http")
            || baseUri.AbsolutePath != "/" || !string.IsNullOrEmpty(baseUri.Query)
            || !string.IsNullOrEmpty(baseUri.Fragment) || !string.IsNullOrEmpty(baseUri.UserInfo))
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "إعداد الموقع العام غير مكتمل",
                detail: "اضبط PublicSite:BaseUrl على عنوان الواجهة العامة.");

        var encodedSlug = Uri.EscapeDataString(slug);
        var invitationUrl = new Uri(baseUri, $"/invite/{encodedSlug}").AbsoluteUri;
        var hasPublicImage = invitation.HasFinalPng
            || invitation.Media.Any(media => media.MediaType == "Image");
        var shareImagePath = !invitation.EventEnded && hasPublicImage
            ? $"/api/v1/public/invitations/{encodedSlug}/share-image"
            : "/share-default.png";
        var shareImageUrl = new Uri(baseUri, shareImagePath).AbsoluteUri;
        var title = invitation.EventEnded
            ? "دعوة زفاف | WedLink"
            : $"دعوة زفاف {invitation.BrideName} و{invitation.GroomName}";
        var description = invitation.EventEnded
            ? "انتهى موعد هذه الدعوة. نتمنى للعروسين حياة مليئة بالفرح والسعادة."
            : $"شاركونا فرحة زفاف {invitation.BrideName} و{invitation.GroomName}. افتحوا الدعوة لمعرفة التفاصيل وتأكيد الحضور.";

        var html = $"""
<!doctype html>
<html lang="ar" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>{HtmlEncoder.Default.Encode(title)}</title>
<meta name="description" content="{HtmlEncoder.Default.Encode(description)}">
<link rel="canonical" href="{HtmlEncoder.Default.Encode(invitationUrl)}">
<meta property="og:type" content="website"><meta property="og:site_name" content="WedLink">
<meta property="og:locale" content="ar_EG"><meta property="og:title" content="{HtmlEncoder.Default.Encode(title)}">
<meta property="og:description" content="{HtmlEncoder.Default.Encode(description)}">
<meta property="og:url" content="{HtmlEncoder.Default.Encode(invitationUrl)}"><meta property="og:image" content="{HtmlEncoder.Default.Encode(shareImageUrl)}">
<meta name="twitter:card" content="summary_large_image"><meta name="twitter:title" content="{HtmlEncoder.Default.Encode(title)}">
<meta name="twitter:description" content="{HtmlEncoder.Default.Encode(description)}"><meta name="twitter:image" content="{HtmlEncoder.Default.Encode(shareImageUrl)}">
<meta http-equiv="refresh" content="0;url={HtmlEncoder.Default.Encode(invitationUrl)}"></head>
<body><p>جارٍ فتح الدعوة… <a href="{HtmlEncoder.Default.Encode(invitationUrl)}">افتح الدعوة</a></p><script>location.replace({System.Text.Json.JsonSerializer.Serialize(invitationUrl)});</script></body></html>
""";
        return Content(html, "text/html; charset=utf-8");
    }

    [AllowAnonymous]
    [HttpGet("public/invitations/{slug}/share-image")]
    [EnableRateLimiting("public-invitation")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> GetShareImage(string slug, CancellationToken cancellationToken)
    {
        PublicInvitationData invitation;
        try { invitation = await invitations.GetPublicAsync(slug, cancellationToken); }
        catch (InvitationNotFoundException) { return NotFound(); }
        if (invitation.EventEnded)
        {
            return RedirectToDefaultShareImage();
        }

        if (invitation.HasFinalPng)
        {
            var card = await invitations.GetPublicFinalPngAsync(slug, cancellationToken);
            if (card is not null)
            {
                var cardStream = await storage.OpenReadAsync(card.StorageKey, cancellationToken);
                if (cardStream is not null) return File(cardStream, card.ContentType);
            }
        }

        var image = invitation.Media.FirstOrDefault(media => media.MediaType == "Image");
        if (image is null) return RedirectToDefaultShareImage();
        var mediaData = await invitations.GetPublicMediaAsync(slug, image.MediaId, cancellationToken);
        if (mediaData is null) return RedirectToDefaultShareImage();
        var stream = await storage.OpenReadAsync(mediaData.StorageKey, cancellationToken);
        return stream is null ? RedirectToDefaultShareImage() : File(stream, mediaData.ContentType);
    }

    private IActionResult RedirectToDefaultShareImage()
    {
        var baseUrl = configuration["PublicSite:BaseUrl"]?.TrimEnd('/');
        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            ? Redirect(new Uri(baseUri, "/share-default.png").AbsoluteUri)
            : NotFound();
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
            var replacedStorageKey = await invitations.SetFinalPngAsync(invitationId, key, cancellationToken);
            if (!string.IsNullOrEmpty(replacedStorageKey) && replacedStorageKey != key)
                await storage.DeleteAsync(replacedStorageKey, cancellationToken);
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
