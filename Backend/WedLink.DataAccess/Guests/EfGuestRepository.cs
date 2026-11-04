using Microsoft.EntityFrameworkCore;
using WedLink.Business.Guests;
using WedLink.DataAccess.Invitations;
using WedLink.DataAccess.Persistence;

namespace WedLink.DataAccess.Guests;

public sealed class EfGuestRepository(WedLinkDbContext db) : IGuestRepository
{
    public async Task<GuestInvitationContext?> GetPublicContextAsync(string slug, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.AsNoTracking().FirstOrDefaultAsync(item => item.Slug == slug
            && item.Status == "Published" && item.ExpiresAt > now
            && db.Orders.Any(order => order.OrderId == item.OrderId && order.DeletedAt == null), cancellationToken);
        if (invitation is null) return null;
        var keys = await (from setting in db.TemplateFeatures.AsNoTracking()
                          join feature in db.Features.AsNoTracking() on setting.FeatureId equals feature.FeatureId
                          where setting.TemplateVersionId == invitation.TemplateVersionId && setting.IsEnabled
                          select new { feature.Key, setting.MaximumCount }).ToListAsync(cancellationToken);
        return new GuestInvitationContext(invitation.InvitationId,
            keys.Any(item => item.Key == "rsvp"), keys.FirstOrDefault(item => item.Key == "rsvp")?.MaximumCount,
            keys.Any(item => item.Key == "guest-count"), keys.Any(item => item.Key == "guest-messages"),
            keys.Any(item => item.Key == "music"));
    }

    public async Task<IReadOnlyList<GuestMessageData>> GetPublicMessagesAsync(int invitationId, CancellationToken cancellationToken = default) =>
        await db.GuestMessages.AsNoTracking().Where(item => item.InvitationId == invitationId && item.DeletedAt == null)
            .OrderByDescending(item => item.SubmittedAt).Take(100)
            .Select(item => new GuestMessageData(item.GuestMessageId, item.GuestName, item.Message, item.SubmittedAt))
            .ToListAsync(cancellationToken);

    public async Task<long> GetExpectedAttendanceAsync(int invitationId, CancellationToken cancellationToken = default) =>
        await db.RsvpResponses.AsNoTracking().Where(item => item.InvitationId == invitationId && item.Response == "Yes")
            .SumAsync(item => (long)item.AccompanyingPeople + 1L, cancellationToken);

    public async Task<GuestRsvpData?> SaveRsvpAsync(int invitationId, string editTokenHash, bool updateOnly,
        GuestRsvpInput input, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var row = await db.RsvpResponses.FirstOrDefaultAsync(item => item.InvitationId == invitationId
            && item.EditTokenHash == editTokenHash, cancellationToken);
        if (row is null && updateOnly) return null;
        var isNew = row is null;
        row ??= new RsvpResponse { InvitationId = invitationId, EditTokenHash = editTokenHash, SubmittedAt = now };
        row.Response = input.Response;
        row.GuestName = input.GuestName;
        row.AccompanyingPeople = input.AccompanyingPeople;
        if (!isNew) row.UpdatedAt = now;
        if (isNew) db.RsvpResponses.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return Map(row);
    }

    public async Task<GuestRsvpSummaryData?> GetRsvpsForAdminAsync(int invitationId, CancellationToken cancellationToken = default)
    {
        if (!await db.Invitations.AnyAsync(item => item.InvitationId == invitationId, cancellationToken)) return null;
        var rows = await db.RsvpResponses.AsNoTracking().Where(item => item.InvitationId == invitationId)
            .OrderByDescending(item => item.SubmittedAt).ToListAsync(cancellationToken);
        var responses = rows.Select(Map).ToArray();
        return new GuestRsvpSummaryData(responses.Count(item => item.Response == "Yes"),
            responses.Count(item => item.Response == "No"), responses.Sum(item => item.ExpectedAttendance), responses);
    }

    public async Task<GuestMessageData?> AddMessageAsync(int invitationId, GuestMessageInput input, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (!await db.Invitations.AnyAsync(item => item.InvitationId == invitationId, cancellationToken)) return null;
        var row = new GuestMessage { InvitationId = invitationId, GuestName = input.GuestName, Message = input.Message, SubmittedAt = now };
        db.GuestMessages.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return new GuestMessageData(row.GuestMessageId, row.GuestName, row.Message, row.SubmittedAt);
    }

    public async Task<IReadOnlyList<AdminGuestMessageData>?> GetMessagesForAdminAsync(int invitationId, CancellationToken cancellationToken = default)
    {
        if (!await db.Invitations.AnyAsync(item => item.InvitationId == invitationId, cancellationToken)) return null;
        return await db.GuestMessages.AsNoTracking().Where(item => item.InvitationId == invitationId)
            .OrderByDescending(item => item.SubmittedAt).Select(item => new AdminGuestMessageData(item.GuestMessageId,
                item.GuestName, item.Message, item.SubmittedAt, item.DeletedAt, item.DeletedByAdminUserId)).ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteMessageAsync(int invitationId, int messageId, string adminUserId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var row = await db.GuestMessages.FirstOrDefaultAsync(item => item.InvitationId == invitationId
            && item.GuestMessageId == messageId && item.DeletedAt == null, cancellationToken);
        if (row is null) return false;
        row.DeletedAt = now;
        row.DeletedByAdminUserId = adminUserId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<MusicTrackData>> GetMusicTracksAsync(bool activeOnly, CancellationToken cancellationToken = default) =>
        await db.MusicTracks.AsNoTracking().Where(item => !activeOnly || item.IsActive).OrderBy(item => item.Title)
            .Select(item => new MusicTrackData(item.MusicTrackId, item.Title, item.Artist, item.RightsReference, item.ContentType, item.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<MusicTrackData> AddMusicTrackAsync(string title, string? artist, string rightsReference,
        string storageKey, string contentType, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var row = new MusicTrack { Title = title, Artist = artist, RightsReference = rightsReference,
            StorageKey = storageKey, ContentType = contentType, IsActive = true, CreatedAt = now };
        db.MusicTracks.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return Map(row);
    }

    public async Task<MusicTrackData?> UpdateMusicTrackAsync(int musicTrackId, string title, string? artist,
        string rightsReference, bool isActive, CancellationToken cancellationToken = default)
    {
        var row = await db.MusicTracks.FirstOrDefaultAsync(item => item.MusicTrackId == musicTrackId, cancellationToken);
        if (row is null) return null;
        row.Title = title; row.Artist = artist; row.RightsReference = rightsReference; row.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        return Map(row);
    }

    public async Task<bool> SetInvitationMusicAsync(int invitationId, int? musicTrackId, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.FirstOrDefaultAsync(item => item.InvitationId == invitationId
            && db.Orders.Any(order => order.OrderId == item.OrderId && order.DeletedAt == null), cancellationToken);
        if (invitation is null || invitation.Status is "Published" or "Paused" or "Expired") return false;
        if (!await IsFeatureEnabledAsync(invitation.TemplateVersionId, "music", cancellationToken)) return false;
        MusicTrack? track = null;
        if (musicTrackId is int id)
        {
            track = await db.MusicTracks.FirstOrDefaultAsync(item => item.MusicTrackId == id && item.IsActive, cancellationToken);
            if (track is null) return false;
        }
        invitation.MusicTrackId = track?.MusicTrackId;
        if (invitation.Status == "Approved") invitation.Status = "Draft";
        invitation.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(MusicTrackData Track, string StorageKey)?> GetInvitationMusicAsync(string slug, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var row = await (from invitation in db.Invitations.AsNoTracking()
                         join track in db.MusicTracks.AsNoTracking() on invitation.MusicTrackId equals track.MusicTrackId
                         where invitation.Slug == slug && invitation.Status == "Published" && invitation.ExpiresAt > now
                             && track.IsActive && db.Orders.Any(order => order.OrderId == invitation.OrderId && order.DeletedAt == null)
                         select new { invitation.TemplateVersionId, track }).FirstOrDefaultAsync(cancellationToken);
        if (row is null || !await IsFeatureEnabledAsync(row.TemplateVersionId, "music", cancellationToken)) return null;
        return (Map(row.track), row.track.StorageKey);
    }

    private async Task<bool> IsFeatureEnabledAsync(int versionId, string key, CancellationToken cancellationToken) =>
        await (from setting in db.TemplateFeatures
               join feature in db.Features on setting.FeatureId equals feature.FeatureId
               where setting.TemplateVersionId == versionId && setting.IsEnabled && feature.Key == key
               select setting.TemplateVersionId).AnyAsync(cancellationToken);

    private static GuestRsvpData Map(RsvpResponse item) => new(item.RsvpResponseId, item.Response, item.GuestName,
        item.AccompanyingPeople, item.Response == "Yes" ? 1L + item.AccompanyingPeople : 0L, item.SubmittedAt, item.UpdatedAt);
    private static MusicTrackData Map(MusicTrack item) => new(item.MusicTrackId, item.Title, item.Artist, item.RightsReference, item.ContentType, item.IsActive);
}
