namespace WedLink.Business.Guests;

public sealed record GuestRsvpInput(string Response, string? GuestName, int AccompanyingPeople);
public sealed record GuestRsvpData(int RsvpResponseId, string Response, string? GuestName, int AccompanyingPeople,
    long ExpectedAttendance, DateTimeOffset SubmittedAt, DateTimeOffset? UpdatedAt);
public sealed record GuestRsvpSubmissionData(GuestRsvpData Response, string EditToken, bool IsUpdate);
public sealed record GuestRsvpSummaryData(int YesCount, int NoCount, long ExpectedAttendance, IReadOnlyList<GuestRsvpData> Responses);
public sealed record GuestMessageInput(string? GuestName, string Message);
public sealed record GuestMessageData(int GuestMessageId, string? GuestName, string Message, DateTimeOffset SubmittedAt);
public sealed record AdminGuestMessageData(int GuestMessageId, string? GuestName, string Message, DateTimeOffset SubmittedAt, DateTimeOffset? DeletedAt, string? DeletedByAdminUserId);
public sealed record MusicTrackData(int MusicTrackId, string Title, string? Artist, string RightsReference, string ContentType, bool IsActive);
public sealed record GuestInteractionData(bool RsvpEnabled, int? MaximumAccompanyingPeople, bool GuestCountEnabled,
    long ExpectedAttendance, bool MessagesEnabled, IReadOnlyList<GuestMessageData> Messages,
    bool MusicEnabled, MusicTrackData? MusicTrack, string? MusicUrl);
public sealed record GuestInvitationContext(int InvitationId, bool RsvpEnabled, int? MaximumAccompanyingPeople,
    bool GuestCountEnabled, bool MessagesEnabled, bool MusicEnabled);

public interface IGuestRepository
{
    Task<GuestInvitationContext?> GetPublicContextAsync(string slug, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GuestMessageData>> GetPublicMessagesAsync(int invitationId, CancellationToken cancellationToken = default);
    Task<long> GetExpectedAttendanceAsync(int invitationId, CancellationToken cancellationToken = default);
    Task<GuestRsvpData?> SaveRsvpAsync(int invitationId, string editTokenHash, bool updateOnly, GuestRsvpInput input, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<GuestRsvpSummaryData?> GetRsvpsForAdminAsync(int invitationId, CancellationToken cancellationToken = default);
    Task<GuestMessageData?> AddMessageAsync(int invitationId, GuestMessageInput input, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminGuestMessageData>?> GetMessagesForAdminAsync(int invitationId, CancellationToken cancellationToken = default);
    Task<bool> DeleteMessageAsync(int invitationId, int messageId, string adminUserId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MusicTrackData>> GetMusicTracksAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<MusicTrackData> AddMusicTrackAsync(string title, string? artist, string rightsReference, string storageKey, string contentType, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<MusicTrackData?> UpdateMusicTrackAsync(int musicTrackId, string title, string? artist, string rightsReference, bool isActive, CancellationToken cancellationToken = default);
    Task<bool> SetInvitationMusicAsync(int invitationId, int? musicTrackId, CancellationToken cancellationToken = default);
    Task<(MusicTrackData Track, string StorageKey)?> GetInvitationMusicAsync(string slug, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed class GuestValidationException(string message) : Exception(message);
public sealed class GuestInvitationNotFoundException() : Exception("الدعوة غير متاحة.");
