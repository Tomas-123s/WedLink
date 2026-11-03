using Microsoft.EntityFrameworkCore;
using WedLink.Business.CustomerSubmissions;
using WedLink.DataAccess.Persistence;

namespace WedLink.DataAccess.Orders;

public sealed class EfCustomerSubmissionRepository(WedLinkDbContext db) : ICustomerSubmissionRepository
{
    public async Task<SubmissionLinkCreatedData?> CreateLinkAsync(int orderId, string tokenHash, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        if (!await db.Orders.AnyAsync(item => item.OrderId == orderId && item.DeletedAt == null, cancellationToken)) return null;
        var current = await db.PrivateLinkTokens.Where(item => item.OrderId == orderId && item.Purpose == "Submission" && item.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var item in current) item.RevokedAt = now;
        var token = new PrivateLinkToken { OrderId = orderId, TokenHash = tokenHash, Purpose = "Submission", CreatedAt = now, ExpiresAt = expiresAt };
        db.PrivateLinkTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
        return new SubmissionLinkCreatedData(token.PrivateLinkTokenId, expiresAt);
    }

    public async Task<bool> RevokeLinkAsync(int orderId, int linkId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var link = await db.PrivateLinkTokens.FirstOrDefaultAsync(item => item.PrivateLinkTokenId == linkId && item.OrderId == orderId && item.Purpose == "Submission" && item.RevokedAt == null, cancellationToken);
        if (link is null) return false;
        link.RevokedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<CurrentSubmissionLinkData?> GetCurrentLinkAsync(int orderId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        db.PrivateLinkTokens.Where(item => item.OrderId == orderId && item.Purpose == "Submission" && item.RevokedAt == null && item.ExpiresAt > now)
            .OrderByDescending(item => item.CreatedAt).Select(item => new CurrentSubmissionLinkData(item.PrivateLinkTokenId, item.ExpiresAt))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int?> GetOrderIdForTokenAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        db.PrivateLinkTokens.Where(item => item.TokenHash == tokenHash && item.Purpose == "Submission" && item.RevokedAt == null && item.ExpiresAt > now)
            .Select(item => (int?)item.OrderId).FirstOrDefaultAsync(cancellationToken);

    public async Task<WeddingSubmissionData?> GetSubmissionAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var orderId = await GetOrderIdForTokenAsync(tokenHash, now, cancellationToken);
        if (orderId is null) return null;
        return await GetByOrderIdAsync(orderId.Value, cancellationToken);
    }

    public async Task<WeddingSubmissionData?> SaveSubmissionAsync(string tokenHash, DateTimeOffset now, SaveWeddingSubmissionInput input, CancellationToken cancellationToken = default)
    {
        var orderId = await GetOrderIdForTokenAsync(tokenHash, now, cancellationToken);
        if (orderId is null) return null;
        var submission = await db.CustomerWeddingSubmissions.FirstOrDefaultAsync(item => item.OrderId == orderId.Value, cancellationToken);
        if (submission is null)
        {
            submission = new CustomerWeddingSubmission { OrderId = orderId.Value, CreatedAt = now };
            db.CustomerWeddingSubmissions.Add(submission);
        }
        submission.BrideName = input.BrideName;
        submission.GroomName = input.GroomName;
        submission.WeddingDateTimeLocal = input.WeddingDateTimeLocal;
        submission.TimeZoneId = input.TimeZoneId;
        submission.VenueName = input.VenueName;
        submission.VenueAddress = input.VenueAddress;
        submission.LocationUrl = input.LocationUrl;
        submission.CustomizationsJson = input.CustomizationsJson;
        submission.UpdatedAt = now;
        if (input.Submit) submission.SubmittedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return await GetByOrderIdAsync(orderId.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<string>?> GetAllowedCustomizationKeysAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var versionId = await (
            from link in db.PrivateLinkTokens
            join order in db.Orders on link.OrderId equals order.OrderId
            where link.TokenHash == tokenHash && link.Purpose == "Submission" && link.RevokedAt == null && link.ExpiresAt > now && order.DeletedAt == null
            select order.SelectedTemplateVersionId).FirstOrDefaultAsync(cancellationToken);
        if (versionId == 0) return null;
        return await (from setting in db.TemplateFeatures.AsNoTracking()
                      join feature in db.Features.AsNoTracking() on setting.FeatureId equals feature.FeatureId
                      where setting.TemplateVersionId == versionId && setting.IsEnabled
                      select feature.Key).ToListAsync(cancellationToken);
    }

    public async Task<long?> GetActiveMediaBytesAsync(int orderId, CancellationToken cancellationToken = default) =>
        await db.CustomerSubmissionMedia.Where(item => item.OrderId == orderId && item.DeletedAt == null)
            .SumAsync(item => (long?)item.SizeBytes, cancellationToken) ?? 0;

    public Task<int> GetActiveMediaCountAsync(int orderId, string mediaKind, CancellationToken cancellationToken = default) =>
        db.CustomerSubmissionMedia.CountAsync(item => item.OrderId == orderId && item.DeletedAt == null && item.MediaKind == mediaKind, cancellationToken);

    public async Task<SubmissionMediaFeatureLimit?> GetMediaFeatureLimitAsync(int orderId, string featureKey, CancellationToken cancellationToken = default)
    {
        var versionId = await db.Orders.Where(item => item.OrderId == orderId && item.DeletedAt == null)
            .Select(item => (int?)item.SelectedTemplateVersionId).FirstOrDefaultAsync(cancellationToken);
        if (versionId is null) return null;
        return await (from setting in db.TemplateFeatures.AsNoTracking()
                      join feature in db.Features.AsNoTracking() on setting.FeatureId equals feature.FeatureId
                      where setting.TemplateVersionId == versionId && feature.Key == featureKey && setting.IsEnabled
                      select new SubmissionMediaFeatureLimit(setting.MaximumCount, setting.MaximumFileSizeMb))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SubmissionMediaData?> AddMediaAsync(int orderId, string originalFileName, string contentType, long sizeBytes, string storageKey, string mediaKind, DateTimeOffset uploadedAt, CancellationToken cancellationToken = default)
    {
        if (!await db.Orders.AnyAsync(item => item.OrderId == orderId && item.DeletedAt == null, cancellationToken)) return null;
        var item = new CustomerSubmissionMedia { OrderId = orderId, OriginalFileName = originalFileName, ContentType = contentType, SizeBytes = sizeBytes, StorageKey = storageKey, MediaKind = mediaKind, UploadedAt = uploadedAt };
        db.CustomerSubmissionMedia.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return ToMedia(item);
    }

    public async Task<(SubmissionMediaData? Media, string? StorageKey)?> GetMediaAsync(int orderId, int mediaId, CancellationToken cancellationToken = default)
    {
        var item = await db.CustomerSubmissionMedia.AsNoTracking().FirstOrDefaultAsync(media => media.OrderId == orderId && media.CustomerSubmissionMediaId == mediaId && media.DeletedAt == null, cancellationToken);
        return item is null ? null : (ToMedia(item), item.StorageKey);
    }

    public async Task<(SubmissionMediaData? Media, string? StorageKey)?> DeleteMediaAsync(int orderId, int mediaId, string? adminUserId, DateTimeOffset deletedAt, CancellationToken cancellationToken = default)
    {
        if (adminUserId is null && await db.CustomerWeddingSubmissions.AnyAsync(item => item.OrderId == orderId && item.AdminReviewedAt != null, cancellationToken)) return null;
        var item = await db.CustomerSubmissionMedia.FirstOrDefaultAsync(media => media.OrderId == orderId && media.CustomerSubmissionMediaId == mediaId && media.DeletedAt == null, cancellationToken);
        if (item is null) return null;
        var result = (ToMedia(item), item.StorageKey);
        item.DeletedAt = deletedAt;
        item.DeletedByAdminUserId = adminUserId;
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    public Task<WeddingSubmissionData?> GetForAdminAsync(int orderId, CancellationToken cancellationToken = default) =>
        GetByOrderIdAsync(orderId, cancellationToken);

    public async Task<bool> MarkReviewedAsync(int orderId, string adminId, DateTimeOffset reviewedAt, CancellationToken cancellationToken = default)
    {
        var submission = await db.CustomerWeddingSubmissions.FirstOrDefaultAsync(item => item.OrderId == orderId, cancellationToken);
        if (submission is null) return false;
        submission.AdminReviewedAt = reviewedAt;
        submission.AdminReviewedByUserId = adminId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<WeddingSubmissionData?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken)
    {
        var row = await (from order in db.Orders.AsNoTracking()
                         join customer in db.Customers.AsNoTracking() on order.CustomerId equals customer.CustomerId
                         join templateVersion in db.TemplateVersions.AsNoTracking() on order.SelectedTemplateVersionId equals templateVersion.TemplateVersionId
                         where order.OrderId == orderId && order.DeletedAt == null
                         join submission in db.CustomerWeddingSubmissions.AsNoTracking() on order.OrderId equals submission.OrderId into submissions
                         from submission in submissions.DefaultIfEmpty()
                         select new { order.OrderId, CustomerName = customer.Name, submission }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return null;
        var media = await db.CustomerSubmissionMedia.AsNoTracking().Where(item => item.OrderId == orderId && item.DeletedAt == null)
            .OrderBy(item => item.UploadedAt).Select(item => new SubmissionMediaData(item.CustomerSubmissionMediaId, item.OrderId, item.OriginalFileName, item.ContentType, item.SizeBytes, item.MediaKind, item.UploadedAt)).ToListAsync(cancellationToken);
        var versionId = await db.Orders.Where(item => item.OrderId == orderId).Select(item => item.SelectedTemplateVersionId).FirstAsync(cancellationToken);
        var features = await (from setting in db.TemplateFeatures.AsNoTracking()
                              join feature in db.Features.AsNoTracking() on setting.FeatureId equals feature.FeatureId
                              where setting.TemplateVersionId == versionId && setting.IsEnabled
                              select new SubmissionFeatureData(feature.Key, feature.Name, feature.Description, setting.MaximumCount, setting.MaximumFileSizeMb))
            .ToListAsync(cancellationToken);
        return new WeddingSubmissionData(row.OrderId, row.CustomerName,
            row.submission?.BrideName ?? "", row.submission?.GroomName ?? "",
            row.submission?.WeddingDateTimeLocal, row.submission?.TimeZoneId ?? "Africa/Cairo",
            row.submission?.VenueName, row.submission?.VenueAddress, row.submission?.LocationUrl,
            row.submission?.CustomizationsJson, row.submission?.SubmittedAt, row.submission?.AdminReviewedAt, media, features);
    }

    private static SubmissionMediaData ToMedia(CustomerSubmissionMedia item) =>
        new(item.CustomerSubmissionMediaId, item.OrderId, item.OriginalFileName, item.ContentType, item.SizeBytes, item.MediaKind, item.UploadedAt);
}
