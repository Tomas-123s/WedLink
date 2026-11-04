using Microsoft.EntityFrameworkCore;
using WedLink.Business.Invitations;
using WedLink.DataAccess.Persistence;

namespace WedLink.DataAccess.Invitations;

public sealed class EfMediaRetentionRepository(WedLinkDbContext db) : IMediaRetentionRepository
{
    public async Task<IReadOnlyList<MediaRetentionBatch>> GetDueMediaBatchesAsync(
        DateTimeOffset now, int batchSize, CancellationToken cancellationToken = default)
    {
        var invitations = await db.Invitations.AsNoTracking()
            .Select(item => new { item.InvitationId, item.OrderId, item.WeddingDateTimeLocal, item.TimeZoneId, item.MediaDeleteAt })
            .ToListAsync(cancellationToken);
        var submissions = await db.CustomerWeddingSubmissions.AsNoTracking()
            .Select(item => new { item.OrderId, item.WeddingDateTimeLocal, item.TimeZoneId })
            .ToListAsync(cancellationToken);
        var submissionByOrder = submissions.ToDictionary(item => item.OrderId);
        var invitationByOrder = invitations.ToDictionary(item => item.OrderId);
        var candidates = new List<(int OrderId, int? InvitationId)>();

        foreach (var invitation in invitations)
        {
            submissionByOrder.TryGetValue(invitation.OrderId, out var submission);
            var weddingDate = invitation.WeddingDateTimeLocal ?? submission?.WeddingDateTimeLocal;
            var timeZoneId = string.IsNullOrWhiteSpace(invitation.TimeZoneId) ? submission?.TimeZoneId : invitation.TimeZoneId;
            if (weddingDate is null || string.IsNullOrWhiteSpace(timeZoneId)) continue;
            var deleteAt = invitation.MediaDeleteAt ?? WeddingTimePolicy.AddCalendarMonths(weddingDate.Value, timeZoneId, 3);
            if (deleteAt <= now) candidates.Add((invitation.OrderId, invitation.InvitationId));
        }

        foreach (var submission in submissions.Where(item => !invitationByOrder.ContainsKey(item.OrderId)))
        {
            if (submission.WeddingDateTimeLocal is null || string.IsNullOrWhiteSpace(submission.TimeZoneId)) continue;
            var deleteAt = WeddingTimePolicy.AddCalendarMonths(submission.WeddingDateTimeLocal.Value, submission.TimeZoneId, 3);
            if (deleteAt <= now) candidates.Add((submission.OrderId, null));
        }

        var dueBatches = new List<MediaRetentionBatch>();
        foreach (var candidate in candidates)
        {
            var customerKeys = await db.CustomerSubmissionMedia.AsNoTracking()
                .Where(item => item.OrderId == candidate.OrderId && item.DeletedAt == null)
                .Select(item => item.StorageKey).ToListAsync(cancellationToken);
            var invitationKeys = candidate.InvitationId is int invitationId
                ? await db.InvitationMedia.AsNoTracking().Where(item => item.InvitationId == invitationId && item.DeletedAt == null)
                    .Select(item => item.StorageKey).ToListAsync(cancellationToken)
                : [];
            var keys = customerKeys.Concat(invitationKeys).Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.Ordinal).ToArray();
            if (keys.Length > 0)
            {
                dueBatches.Add(new MediaRetentionBatch(candidate.OrderId, candidate.InvitationId, keys));
                if (dueBatches.Count >= Math.Max(1, batchSize)) break;
            }
        }
        return dueBatches;
    }

    public async Task<int> MarkMediaDeletedAsync(int orderId, int? invitationId, IReadOnlyList<string> storageKeys,
        DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (storageKeys.Count == 0) return 0;
        var customerRows = await db.CustomerSubmissionMedia
            .Where(item => item.OrderId == orderId && item.DeletedAt == null && storageKeys.Contains(item.StorageKey))
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.DeletedAt, now), cancellationToken);
        var invitationRows = invitationId is int id
            ? await db.InvitationMedia.Where(item => item.InvitationId == id && item.DeletedAt == null && storageKeys.Contains(item.StorageKey))
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.DeletedAt, now), cancellationToken)
            : 0;
        return customerRows + invitationRows;
    }
}
