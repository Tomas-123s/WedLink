using System.ComponentModel.DataAnnotations;
using WedLink.Business.Guests;

namespace WedLink.API.Contracts.Guests;

public sealed record GuestRsvpRequest([Required] string Response, [StringLength(150)] string? GuestName,
    [Range(0, int.MaxValue - 1)] int AccompanyingPeople, [StringLength(100)] string? EditToken);
public sealed record GuestRsvpResponse(int RsvpResponseId, string Response, string? GuestName, int AccompanyingPeople,
    int ExpectedAttendance, DateTimeOffset SubmittedAt, DateTimeOffset? UpdatedAt, string EditToken, bool IsUpdate)
{
    public static GuestRsvpResponse FromData(GuestRsvpSubmissionData item) => new(item.Response.RsvpResponseId,
        item.Response.Response, item.Response.GuestName, item.Response.AccompanyingPeople,
        item.Response.ExpectedAttendance, item.Response.SubmittedAt, item.Response.UpdatedAt, item.EditToken, item.IsUpdate);
}
public sealed record RsvpItemResponse(int RsvpResponseId, string Response, string? GuestName, int AccompanyingPeople,
    int ExpectedAttendance, DateTimeOffset SubmittedAt, DateTimeOffset? UpdatedAt);
public sealed record RsvpSummaryResponse(int YesCount, int NoCount, int ExpectedAttendance, IReadOnlyList<RsvpItemResponse> Responses)
{
    public static RsvpSummaryResponse FromData(GuestRsvpSummaryData item) => new(item.YesCount, item.NoCount,
        item.ExpectedAttendance, item.Responses.Select(row => new RsvpItemResponse(row.RsvpResponseId, row.Response,
            row.GuestName, row.AccompanyingPeople, row.ExpectedAttendance, row.SubmittedAt, row.UpdatedAt)).ToArray());
}
public sealed record GuestMessageRequest([StringLength(150)] string? GuestName, [Required, StringLength(2000, MinimumLength = 1)] string Message);
public sealed record GuestMessageResponse(int GuestMessageId, string? GuestName, string Message, DateTimeOffset SubmittedAt);
public sealed record AdminGuestMessageResponse(int GuestMessageId, string? GuestName, string Message, DateTimeOffset SubmittedAt,
    DateTimeOffset? DeletedAt, string? DeletedByAdminUserId);
public sealed record PublicGuestInteractionsResponse(bool RsvpEnabled, int? MaximumAccompanyingPeople, bool GuestCountEnabled,
    int ExpectedAttendance, bool MessagesEnabled, IReadOnlyList<GuestMessageResponse> Messages,
    bool MusicEnabled, PublicMusicTrackResponse? MusicTrack, string? MusicUrl)
{
    public static PublicGuestInteractionsResponse FromData(GuestInteractionData item) => new(item.RsvpEnabled,
        item.MaximumAccompanyingPeople, item.GuestCountEnabled, item.ExpectedAttendance, item.MessagesEnabled,
        item.Messages.Select(message => new GuestMessageResponse(message.GuestMessageId, message.GuestName, message.Message, message.SubmittedAt)).ToArray(),
        item.MusicEnabled, item.MusicTrack is null ? null : PublicMusicTrackResponse.FromData(item.MusicTrack), item.MusicUrl);
}
public sealed record MusicTrackResponse(int MusicTrackId, string Title, string? Artist, string RightsReference, bool IsActive)
{
    public static MusicTrackResponse FromData(MusicTrackData item) => new(item.MusicTrackId, item.Title, item.Artist, item.RightsReference, item.IsActive);
}
public sealed record PublicMusicTrackResponse(int MusicTrackId, string Title, string? Artist)
{
    public static PublicMusicTrackResponse FromData(MusicTrackData item) => new(item.MusicTrackId, item.Title, item.Artist);
}
public sealed record UpdateMusicTrackRequest([Required, StringLength(200, MinimumLength = 1)] string Title,
    [StringLength(150)] string? Artist, [Required, StringLength(1000, MinimumLength = 1)] string RightsReference, bool IsActive);
public sealed record SelectMusicTrackRequest(int? MusicTrackId);
