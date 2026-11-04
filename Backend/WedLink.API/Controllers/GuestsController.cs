using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WedLink.API.Contracts.Guests;
using WedLink.Business.CustomerSubmissions;
using WedLink.Business.Guests;

namespace WedLink.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class GuestsController(GuestService guests, ISubmissionFileStorage storage) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/invitations/{slug}/guest-interactions")]
    [EnableRateLimiting("public-invitation")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> GetInteractions(string slug, CancellationToken cancellationToken)
    {
        try { return Ok(PublicGuestInteractionsResponse.FromData(await guests.GetPublicInteractionsAsync(slug, cancellationToken))); }
        catch (GuestInvitationNotFoundException exception) { return NotFoundProblem(exception.Message); }
    }

    [AllowAnonymous]
    [HttpPost("public/invitations/{slug}/rsvps")]
    [EnableRateLimiting("guest-rsvp")]
    public async Task<IActionResult> SubmitRsvp(string slug, GuestRsvpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await guests.SubmitRsvpAsync(slug, request.EditToken,
                new GuestRsvpInput(request.Response, request.GuestName, request.AccompanyingPeople), cancellationToken);
            return Ok(GuestRsvpResponse.FromData(result));
        }
        catch (GuestInvitationNotFoundException exception) { return NotFoundProblem(exception.Message); }
        catch (GuestValidationException exception) { return ValidationProblem(exception.Message); }
    }

    [AllowAnonymous]
    [HttpPost("public/invitations/{slug}/messages")]
    [EnableRateLimiting("guest-messages")]
    public async Task<IActionResult> SubmitMessage(string slug, GuestMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await guests.AddMessageAsync(slug, new GuestMessageInput(request.GuestName, request.Message), cancellationToken);
            return Created($"/api/v1/public/invitations/{slug}/guest-interactions", new GuestMessageResponse(item.GuestMessageId, item.GuestName, item.Message, item.SubmittedAt));
        }
        catch (GuestInvitationNotFoundException exception) { return NotFoundProblem(exception.Message); }
        catch (GuestValidationException exception) { return ValidationProblem(exception.Message); }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/invitations/{invitationId:int}/rsvps")]
    public async Task<IActionResult> GetRsvps(int invitationId, CancellationToken cancellationToken)
    {
        var summary = await guests.GetRsvpsForAdminAsync(invitationId, cancellationToken);
        return summary is null ? NotFound() : Ok(RsvpSummaryResponse.FromData(summary));
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/invitations/{invitationId:int}/messages")]
    public async Task<IActionResult> GetMessages(int invitationId, CancellationToken cancellationToken)
    {
        var messages = await guests.GetMessagesForAdminAsync(invitationId, cancellationToken);
        return messages is null ? NotFound() : Ok(messages.Select(item => new AdminGuestMessageResponse(item.GuestMessageId,
            item.GuestName, item.Message, item.SubmittedAt, item.DeletedAt, item.DeletedByAdminUserId)).ToArray());
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpDelete("admin/invitations/{invitationId:int}/messages/{messageId:int}")]
    public async Task<IActionResult> DeleteMessage(int invitationId, int messageId, CancellationToken cancellationToken)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(adminId)) return Forbid();
        return await guests.DeleteMessageAsync(invitationId, messageId, adminId, cancellationToken) ? NoContent() : NotFound();
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/music-tracks")]
    public async Task<IActionResult> GetTracks(CancellationToken cancellationToken) =>
        Ok((await guests.GetMusicTracksAsync(false, cancellationToken)).Select(MusicTrackResponse.FromData));

    [AllowAnonymous]
    [HttpGet("public/music-tracks")]
    [EnableRateLimiting("public-invitation")]
    public async Task<IActionResult> GetActiveTracks(CancellationToken cancellationToken) =>
        Ok((await guests.GetMusicTracksAsync(true, cancellationToken)).Select(PublicMusicTrackResponse.FromData));

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("admin/music-tracks")]
    [RequestSizeLimit(21 * 1024 * 1024)]
    public async Task<IActionResult> AddTrack(IFormFile file, [FromForm] string title, [FromForm] string? artist,
        [FromForm] string rightsReference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200 || artist?.Length > 150
            || string.IsNullOrWhiteSpace(rightsReference) || rightsReference.Trim().Length > 1000)
            return ValidationProblem("أدخل عنوانًا وبيانات حقوق صالحة للمسار.");
        if (file is null || file.Length > 20L * 1024 * 1024) return ValidationProblem("حجم المسار يتجاوز 20MB.");
        await using var stream = file.OpenReadStream();
        (string Kind, string ContentType) details;
        try { details = MediaFilePolicy.Validate(file.FileName, file.ContentType, file.Length, stream); }
        catch (SubmissionValidationException exception) { return ValidationProblem(exception.Message); }
        if (details.Kind != "Audio") return ValidationProblem("ارفع ملف MP3 أو M4A مرخصًا للاستخدام.");
        var key = await storage.SaveAsync(stream, cancellationToken);
        try
        {
            var track = await guests.AddMusicTrackAsync(title.Trim(), artist, rightsReference, key, details.ContentType, cancellationToken);
            return Created($"/api/v1/admin/music-tracks/{track.MusicTrackId}", MusicTrackResponse.FromData(track));
        }
        catch { await storage.DeleteAsync(key, cancellationToken); throw; }
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("admin/music-tracks/{musicTrackId:int}")]
    public async Task<IActionResult> UpdateTrack(int musicTrackId, UpdateMusicTrackRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.RightsReference))
            return ValidationProblem("العنوان وبيانات الحقوق مطلوبان.");
        var item = await guests.UpdateMusicTrackAsync(musicTrackId, request.Title, request.Artist,
            request.RightsReference, request.IsActive, cancellationToken);
        return item is null ? NotFound() : Ok(MusicTrackResponse.FromData(item));
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("admin/invitations/{invitationId:int}/music")]
    public async Task<IActionResult> SetInvitationMusic(int invitationId, SelectMusicTrackRequest request, CancellationToken cancellationToken) =>
        await guests.SetInvitationMusicAsync(invitationId, request.MusicTrackId, cancellationToken) ? NoContent() : ValidationProblem("تعذر اختيار الموسيقى. تأكد من أن القالب والدعوة والمسار متاحون.");

    [AllowAnonymous]
    [HttpGet("public/invitations/{slug}/music")]
    [EnableRateLimiting("public-invitation")]
    public async Task<IActionResult> GetInvitationMusic(string slug, CancellationToken cancellationToken)
    {
        var item = await guests.GetInvitationMusicAsync(slug, cancellationToken);
        if (item is null) return NotFound();
        var stream = await storage.OpenReadAsync(item.Value.StorageKey, cancellationToken);
        return stream is null ? NotFound() : File(stream, item.Value.Track.ContentType);
    }

    private ObjectResult ValidationProblem(string detail) => Problem(statusCode: StatusCodes.Status400BadRequest, title: "بيانات غير صالحة", detail: detail);
    private ObjectResult NotFoundProblem(string detail) => Problem(statusCode: StatusCodes.Status404NotFound, title: "الدعوة غير متاحة", detail: detail);
}
