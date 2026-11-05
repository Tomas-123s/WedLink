using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WedLink.Business.Invitations;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Persistence;

namespace WedLink.DataAccess.Invitations;

public sealed class EfInvitationRepository(WedLinkDbContext db, TimeProvider timeProvider) : IInvitationRepository, IInvitationExpirationRepository
{
    private const string PreviewPurpose = "InvitationPreview";

    public async Task<InvitationData?> GetForAdminByInvitationIdAsync(int invitationId, CancellationToken cancellationToken = default)
    {
        var orderId = await db.Invitations.AsNoTracking().Where(item => item.InvitationId == invitationId)
            .Select(item => (int?)item.OrderId).FirstOrDefaultAsync(cancellationToken);
        return orderId is null ? null : await GetForAdminAsync(orderId.Value, cancellationToken);
    }

    public async Task<InvitationLifecycleFailure> PublishAsync(int invitationId, string slug, DateTimeOffset now,
        DateTimeOffset expiresAt, DateTimeOffset mediaDeleteAt, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.FirstOrDefaultAsync(item => item.InvitationId == invitationId
            && db.Orders.Any(order => order.OrderId == item.OrderId && order.DeletedAt == null), cancellationToken);
        if (invitation is null) return InvitationLifecycleFailure.NotFound;
        if (invitation.Status != "Approved")
        {
            if (!await HasCurrentCustomerApprovalAsync(invitation, cancellationToken))
                return InvitationLifecycleFailure.NotApproved;
            invitation.Status = "Approved";
        }
        if (invitation.WeddingDateTimeLocal is null) return InvitationLifecycleFailure.MissingWeddingDate;
        if (await db.Invitations.AnyAsync(item => item.Slug == slug && item.InvitationId != invitationId, cancellationToken))
            return InvitationLifecycleFailure.SlugConflict;
        invitation.Slug = slug;
        invitation.Status = "Published";
        invitation.PublishedAt = now;
        invitation.ExpiresAt = expiresAt;
        invitation.MediaDeleteAt = mediaDeleteAt;
        invitation.PausedAt = null;
        invitation.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return InvitationLifecycleFailure.None;
    }

    public async Task<InvitationLifecycleFailure> SetPublicationStateAsync(int invitationId, string action, DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.FirstOrDefaultAsync(item => item.InvitationId == invitationId
            && db.Orders.Any(order => order.OrderId == item.OrderId && order.DeletedAt == null), cancellationToken);
        if (invitation is null) return InvitationLifecycleFailure.NotFound;
        if (invitation.ExpiresAt is null || invitation.ExpiresAt <= now)
        {
            if (invitation.PublishedAt is not null && invitation.Status != "Expired")
            {
                invitation.Status = "Expired";
                invitation.PausedAt = null;
                invitation.UpdatedAt = now;
                await db.SaveChangesAsync(cancellationToken);
            }
            return InvitationLifecycleFailure.Expired;
        }
        if (action == "Pause" && invitation.Status == "Published")
        {
            invitation.Status = "Paused";
            invitation.PausedAt = now;
        }
        else if (action == "Reactivate" && invitation.Status == "Paused")
        {
            invitation.Status = "Published";
            invitation.PausedAt = null;
        }
        else return InvitationLifecycleFailure.InvalidState;
        invitation.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return InvitationLifecycleFailure.None;
    }

    public async Task<InvitationLifecycleFailure> ExtendAsync(int invitationId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.FirstOrDefaultAsync(item => item.InvitationId == invitationId
            && db.Orders.Any(order => order.OrderId == item.OrderId && order.DeletedAt == null), cancellationToken);
        if (invitation is null) return InvitationLifecycleFailure.NotFound;
        if (invitation.Status is not ("Published" or "Paused") || invitation.ExpiresAt is null)
            return InvitationLifecycleFailure.InvalidState;
        if (invitation.ExpiresAt <= now)
        {
            invitation.Status = "Expired";
            invitation.PausedAt = null;
            invitation.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return InvitationLifecycleFailure.Expired;
        }
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(invitation.TimeZoneId);
            var localExpiry = TimeZoneInfo.ConvertTime(invitation.ExpiresAt.Value, zone).DateTime;
            invitation.ExpiresAt = WeddingTimePolicy.AddCalendarMonths(localExpiry, invitation.TimeZoneId, 1);
        }
        catch (TimeZoneNotFoundException) { return InvitationLifecycleFailure.InvalidState; }
        catch (InvalidTimeZoneException) { return InvitationLifecycleFailure.InvalidState; }
        invitation.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return InvitationLifecycleFailure.None;
    }

    public async Task<PublicInvitationData?> GetPublicAsync(string slug, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var row = await (from invitation in db.Invitations.AsNoTracking()
                         join order in db.Orders.AsNoTracking() on invitation.OrderId equals order.OrderId
                         join version in db.TemplateVersions.AsNoTracking() on invitation.TemplateVersionId equals version.TemplateVersionId
                         join template in db.Templates.AsNoTracking() on version.TemplateId equals template.TemplateId
                         where invitation.Slug == slug && order.DeletedAt == null
                         select new { invitation, template.Name, version.PackageKey }).FirstOrDefaultAsync(cancellationToken);
        if (row is null || row.invitation.Status is "Expired" || row.invitation.ExpiresAt <= now)
            return row is null ? null : new PublicInvitationData(true, row.invitation.FinalPngStorageKey is not null, "", "default", "", "", null, "Africa/Cairo", null, null, null, null, [], []);
        if (row.invitation.Status != "Published") return null;
        var (bride, groom) = ParseCoupleNames(row.invitation.CoupleNames);
        var features = await (from setting in db.TemplateFeatures.AsNoTracking()
                              join feature in db.Features.AsNoTracking() on setting.FeatureId equals feature.FeatureId
                              where setting.TemplateVersionId == row.invitation.TemplateVersionId && setting.IsEnabled
                              select new InvitationFeatureData(feature.Key, feature.Name, true, setting.MaximumCount, setting.MaximumFileSizeMb))
            .ToListAsync(cancellationToken);
        var customizations = await GetCustomizationsAsync(row.invitation.InvitationId, cancellationToken);
        var media = await db.InvitationMedia.AsNoTracking().Where(item => item.InvitationId == row.invitation.InvitationId && item.DeletedAt == null)
            .OrderBy(item => item.SortOrder).Select(item => new PublicInvitationMediaData(item.InvitationMediaId,
                item.MediaType, item.OriginalFileName, item.ContentType, item.StorageKey)).ToListAsync(cancellationToken);
        return new PublicInvitationData(false, row.invitation.FinalPngStorageKey is not null, row.Name, ResolvePublicRendererKey(row.PackageKey), bride, groom, row.invitation.WeddingDateTimeLocal,
            row.invitation.TimeZoneId, row.invitation.VenueName, row.invitation.VenueAddress, row.invitation.LocationUrl,
            customizations, features, media);
    }

    private static string ResolvePublicRendererKey(string packageKey) =>
        packageKey.StartsWith("editorial-luxury", StringComparison.OrdinalIgnoreCase)
        || packageKey.StartsWith("cinematic", StringComparison.OrdinalIgnoreCase)
            ? "editorial-luxury"
            : "default";

    public async Task<PublicMediaContentData?> GetPublicMediaAsync(string slug, int mediaId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.AsNoTracking().FirstOrDefaultAsync(item => item.Slug == slug
            && item.Status == "Published" && item.ExpiresAt > now
            && db.Orders.Any(order => order.OrderId == item.OrderId && order.DeletedAt == null), cancellationToken);
        if (invitation is null) return null;
        return await db.InvitationMedia.AsNoTracking().Where(item => item.InvitationId == invitation.InvitationId
            && item.InvitationMediaId == mediaId && item.DeletedAt == null)
            .Select(item => new PublicMediaContentData(item.StorageKey, item.ContentType, item.OriginalFileName)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(InvitationLifecycleFailure Failure, string? ReplacedStorageKey)> SetFinalPngAsync(int invitationId, string storageKey, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.FirstOrDefaultAsync(item => item.InvitationId == invitationId
            && db.Orders.Any(order => order.OrderId == item.OrderId && order.DeletedAt == null), cancellationToken);
        if (invitation is null) return (InvitationLifecycleFailure.NotFound, null);
        var replacedStorageKey = invitation.FinalPngStorageKey;
        if (invitation.Status is not ("Approved" or "Published" or "Paused"))
        {
            if (!await HasCurrentCustomerApprovalAsync(invitation, cancellationToken))
                return (InvitationLifecycleFailure.InvalidState, null);
            invitation.Status = "Approved";
        }
        invitation.FinalPngStorageKey = storageKey;
        invitation.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return (InvitationLifecycleFailure.None, replacedStorageKey);
    }

    public async Task<InvitationCardData?> GetFinalPngAsync(int invitationId, CancellationToken cancellationToken = default) =>
        await db.Invitations.AsNoTracking().Where(item => item.InvitationId == invitationId && item.FinalPngStorageKey != null
            && db.Orders.Any(order => order.OrderId == item.OrderId && order.DeletedAt == null))
            .Select(item => new InvitationCardData(item.FinalPngStorageKey!, "image/png")).FirstOrDefaultAsync(cancellationToken);

    public async Task<InvitationCardData?> GetPublicFinalPngAsync(string slug, CancellationToken cancellationToken = default) =>
        await db.Invitations.AsNoTracking().Where(item => item.Slug == slug && item.FinalPngStorageKey != null
            && db.Orders.Any(order => order.OrderId == item.OrderId && order.DeletedAt == null))
            .Select(item => new InvitationCardData(item.FinalPngStorageKey!, "image/png")).FirstOrDefaultAsync(cancellationToken);

    public Task<int> ExpireDueInvitationsAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        db.Invitations.Where(item => item.ExpiresAt != null && item.ExpiresAt <= now
            && (item.Status == "Published" || item.Status == "Paused"))
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.Status, "Expired")
                .SetProperty(item => item.PausedAt, (DateTimeOffset?)null)
                .SetProperty(item => item.UpdatedAt, now), cancellationToken);

    public async Task<InvitationData?> GetForAdminAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var row = await (from order in db.Orders.AsNoTracking()
                         join version in db.TemplateVersions.AsNoTracking() on order.SelectedTemplateVersionId equals version.TemplateVersionId
                         join template in db.Templates.AsNoTracking() on version.TemplateId equals template.TemplateId
                         join submission in db.CustomerWeddingSubmissions.AsNoTracking() on order.OrderId equals submission.OrderId into submissions
                         from submission in submissions.DefaultIfEmpty()
                         join invitation in db.Invitations.AsNoTracking() on order.OrderId equals invitation.OrderId into invitations
                         from invitation in invitations.DefaultIfEmpty()
                         where order.OrderId == orderId && order.DeletedAt == null
                         select new { order.SelectedTemplateVersionId, template.Name, version.VersionNumber, submission, invitation })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var features = await (from setting in db.TemplateFeatures.AsNoTracking()
                              join feature in db.Features.AsNoTracking() on setting.FeatureId equals feature.FeatureId
                              where setting.TemplateVersionId == row.SelectedTemplateVersionId
                              select new InvitationFeatureData(feature.Key, feature.Name, setting.IsEnabled, setting.MaximumCount, setting.MaximumFileSizeMb))
            .ToListAsync(cancellationToken);
        var availableMedia = await db.CustomerSubmissionMedia.AsNoTracking()
            .Where(item => item.OrderId == orderId && item.DeletedAt == null)
            .OrderBy(item => item.UploadedAt)
            .Select(item => new InvitationMediaData(item.CustomerSubmissionMediaId, item.MediaKind, item.OriginalFileName,
                item.ContentType, item.SizeBytes, 0))
            .ToListAsync(cancellationToken);

        var customizations = row.invitation is null
            ? row.submission?.CustomizationsJson
            : await GetCustomizationsAsync(row.invitation.InvitationId, cancellationToken);
        var selectedMediaIds = row.invitation is null
            ? Array.Empty<int>()
            : await (from invitationMedia in db.InvitationMedia.AsNoTracking()
                     join submissionMedia in db.CustomerSubmissionMedia.AsNoTracking()
                         on invitationMedia.StorageKey equals submissionMedia.StorageKey
                     where invitationMedia.InvitationId == row.invitation.InvitationId
                         && invitationMedia.DeletedAt == null
                         && submissionMedia.OrderId == orderId
                         && submissionMedia.DeletedAt == null
                     orderby invitationMedia.SortOrder
                     select submissionMedia.CustomerSubmissionMediaId).ToArrayAsync(cancellationToken);
        var reviewEvents = row.invitation is null
            ? Array.Empty<PreviewReviewEventData>()
            : (await GetEventsAsync(row.invitation.InvitationId, cancellationToken)).ToArray();

        var (brideName, groomName) = row.invitation is null
            ? (row.submission?.BrideName ?? "", row.submission?.GroomName ?? "")
            : ParseCoupleNames(row.invitation.CoupleNames);
        var currentStatus = row.invitation?.Status ?? "Draft";
        if (row.invitation is not null && currentStatus != "Approved"
            && await HasCurrentCustomerApprovalAsync(row.invitation, cancellationToken))
            currentStatus = "Approved";
        if (row.invitation?.ExpiresAt is DateTimeOffset expiry && expiry <= timeProvider.GetUtcNow()
            && currentStatus is ("Published" or "Paused")) currentStatus = "Expired";
        return new InvitationData(orderId, row.invitation?.InvitationId, row.SelectedTemplateVersionId, row.invitation?.MusicTrackId, row.Name,
            row.VersionNumber, brideName, groomName,
            row.invitation?.WeddingDateTimeLocal ?? row.submission?.WeddingDateTimeLocal,
            row.invitation?.TimeZoneId ?? row.submission?.TimeZoneId ?? "Africa/Cairo",
            row.invitation?.VenueName ?? row.submission?.VenueName, row.invitation?.VenueAddress ?? row.submission?.VenueAddress,
            row.invitation?.LocationUrl ?? row.submission?.LocationUrl, customizations,
            currentStatus, row.invitation?.Slug ?? string.Empty, row.invitation?.PublishedAt,
            row.invitation?.ExpiresAt, row.invitation?.PausedAt, row.invitation?.UpdatedAt,
            row.invitation?.FinalPngStorageKey is not null,
            features, availableMedia, selectedMediaIds, reviewEvents);
    }

    public async Task<InvitationSaveResult> SaveAsync(int orderId, SaveInvitationInput input, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var order = await db.Orders.FirstOrDefaultAsync(item => item.OrderId == orderId && item.DeletedAt == null, cancellationToken);
        if (order is null) return new(null, InvitationSaveFailure.OrderNotFound);
        var invitation = await db.Invitations.FirstOrDefaultAsync(item => item.OrderId == orderId, cancellationToken);
        if (invitation is null)
        {
            invitation = new Invitation
            {
                OrderId = orderId,
                TemplateVersionId = order.SelectedTemplateVersionId,
                Slug = string.Empty,
                CreatedAt = now
            };
            db.Invitations.Add(invitation);
        }
        else if (invitation.Status == "Expired"
            || ((invitation.Status is "Published" or "Paused") && invitation.ExpiresAt <= now))
        {
            return new(null, InvitationSaveFailure.InvitationLocked);
        }

        var allowedMedia = await db.CustomerSubmissionMedia.Where(item => item.OrderId == orderId && item.DeletedAt == null && input.MediaIds.Contains(item.CustomerSubmissionMediaId))
            .ToDictionaryAsync(item => item.CustomerSubmissionMediaId, cancellationToken);
        if (allowedMedia.Count != input.MediaIds.Distinct().Count())
            return new(null, InvitationSaveFailure.MediaUnavailable);
        var enabledFeatures = await (from setting in db.TemplateFeatures
                                     join feature in db.Features on setting.FeatureId equals feature.FeatureId
                                     where setting.TemplateVersionId == order.SelectedTemplateVersionId && setting.IsEnabled
                                     select feature.Key).ToListAsync(cancellationToken);
        foreach (var media in allowedMedia.Values)
        {
            var key = media.MediaKind switch { "Image" => "gallery", "Video" => "video", "Audio" => "music", _ => "" };
            if (!enabledFeatures.Contains(key, StringComparer.Ordinal))
                return new(null, InvitationSaveFailure.UnsupportedFeature);
        }

        var existingCustomizations = await db.InvitationCustomizations
            .Where(item => item.InvitationId == invitation.InvitationId).ToListAsync(cancellationToken);
        var invitationMedia = await db.InvitationMedia
            .Where(item => item.InvitationId == invitation.InvitationId && item.DeletedAt == null).ToListAsync(cancellationToken);
        var orderedSelectedMedia = input.MediaIds.Select(mediaId => allowedMedia[mediaId]).ToArray();
        var contentUnchanged = invitation.InvitationId > 0
            && HasSameApprovedContent(invitation, input, orderedSelectedMedia, invitationMedia, existingCustomizations);
        var keepsApproval = invitation.Status == "Approved" && contentUnchanged;
        var remainsPublished = invitation.Status == "Published";
        var remainsPaused = invitation.Status == "Paused";
        var replacedFinalPngStorageKey = !contentUnchanged ? invitation.FinalPngStorageKey : null;

        invitation.CoupleNames = JsonSerializer.Serialize(new[] { input.BrideName, input.GroomName },
            new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        invitation.WeddingDateTimeLocal = input.WeddingDateTimeLocal;
        invitation.TimeZoneId = input.TimeZoneId;
        invitation.VenueName = input.VenueName;
        invitation.VenueAddress = input.VenueAddress;
        invitation.LocationUrl = input.LocationUrl;
        invitation.Status = remainsPublished ? "Published" : remainsPaused ? "Paused" : keepsApproval ? "Approved" : "Draft";
        if (!contentUnchanged) invitation.FinalPngStorageKey = null;
        invitation.UpdatedAt = now;

        if (existingCustomizations.Count > 0) db.InvitationCustomizations.RemoveRange(existingCustomizations);
        // Save the invitation first when it is new so its generated key is available for children.
        await db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(input.CustomizationsJson))
        {
            using var document = JsonDocument.Parse(input.CustomizationsJson);
            foreach (var property in document.RootElement.EnumerateObject())
                db.InvitationCustomizations.Add(new InvitationCustomization
                {
                    InvitationId = invitation.InvitationId,
                    Key = property.Name,
                    ValueJson = property.Value.GetRawText()
                });
        }

        var selectedStorageKeys = allowedMedia.Values.Select(item => item.StorageKey).ToHashSet(StringComparer.Ordinal);
        foreach (var existing in invitationMedia)
        {
            if (!selectedStorageKeys.Contains(existing.StorageKey)) existing.DeletedAt = now;
        }
        var existingByStorageKey = invitationMedia.ToDictionary(item => item.StorageKey, StringComparer.Ordinal);
        for (var sortOrder = 0; sortOrder < input.MediaIds.Count; sortOrder++)
        {
            var media = allowedMedia[input.MediaIds[sortOrder]];
            if (existingByStorageKey.TryGetValue(media.StorageKey, out var existing))
            {
                existing.SortOrder = sortOrder;
                continue;
            }
            db.InvitationMedia.Add(new InvitationMedia
            {
                InvitationId = invitation.InvitationId,
                MediaType = media.MediaKind,
                OriginalFileName = media.OriginalFileName,
                ContentType = media.ContentType,
                SizeBytes = media.SizeBytes,
                StorageKey = media.StorageKey,
                SortOrder = sortOrder,
                UploadedAt = now
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        return new(await GetForAdminAsync(orderId, cancellationToken), InvitationSaveFailure.None, replacedFinalPngStorageKey);
    }

    private static bool HasSameApprovedContent(Invitation invitation, SaveInvitationInput input,
        IEnumerable<CustomerSubmissionMedia> selectedSources, IReadOnlyCollection<InvitationMedia> currentMedia,
        IReadOnlyCollection<InvitationCustomization> currentCustomizations)
    {
        string[]? names;
        try { names = JsonSerializer.Deserialize<string[]>(invitation.CoupleNames); }
        catch (JsonException) { return false; }
        if (names is not { Length: 2 }
            || !string.Equals(names[0], input.BrideName, StringComparison.Ordinal)
            || !string.Equals(names[1], input.GroomName, StringComparison.Ordinal)
            || invitation.WeddingDateTimeLocal != input.WeddingDateTimeLocal
            || !string.Equals(invitation.TimeZoneId, input.TimeZoneId, StringComparison.Ordinal)
            || !string.Equals(invitation.VenueName, input.VenueName, StringComparison.Ordinal)
            || !string.Equals(invitation.VenueAddress, input.VenueAddress, StringComparison.Ordinal)
            || !string.Equals(invitation.LocationUrl, input.LocationUrl, StringComparison.Ordinal))
            return false;

        Dictionary<string, string> requestedCustomizations;
        try
        {
            if (string.IsNullOrWhiteSpace(input.CustomizationsJson))
                requestedCustomizations = new Dictionary<string, string>(StringComparer.Ordinal);
            else
            {
                using var document = JsonDocument.Parse(input.CustomizationsJson);
                requestedCustomizations = document.RootElement.EnumerateObject().ToDictionary(
                    property => property.Name, property => property.Value.GetRawText(), StringComparer.Ordinal);
            }
        }
        catch (JsonException) { return false; }
        if (requestedCustomizations.Count != currentCustomizations.Count
            || currentCustomizations.Any(item => !requestedCustomizations.TryGetValue(item.Key, out var value)
                || !string.Equals(value, item.ValueJson, StringComparison.Ordinal)))
            return false;

        var requestedMediaKeys = selectedSources.Select(item => item.StorageKey).ToArray();
        var currentMediaKeys = currentMedia.OrderBy(item => item.SortOrder).Select(item => item.StorageKey).ToArray();
        return requestedMediaKeys.SequenceEqual(currentMediaKeys, StringComparer.Ordinal);
    }

    private async Task<bool> HasCurrentCustomerApprovalAsync(Invitation invitation, CancellationToken cancellationToken)
    {
        var latestReview = await db.PreviewReviewEvents.AsNoTracking()
            .Where(item => item.InvitationId == invitation.InvitationId && item.Source == "Customer")
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.PreviewReviewEventId)
            .Select(item => new { item.Action, item.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);
        return latestReview?.Action == "Approved"
            && (invitation.UpdatedAt is null || latestReview.CreatedAt >= invitation.UpdatedAt.Value);
    }

    public async Task<CreatedPreviewLinkData?> CreatePreviewLinkAsync(int orderId, string tokenHash, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.FirstOrDefaultAsync(item => item.OrderId == orderId
            && db.Orders.Any(order => order.OrderId == item.OrderId && order.DeletedAt == null), cancellationToken);
        if (invitation is null || invitation.Status is "Published" or "Expired") return null;
        var current = await db.PrivateLinkTokens.Where(item => item.OrderId == orderId && item.Purpose == PreviewPurpose && item.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var item in current) item.RevokedAt = now;
        db.PrivateLinkTokens.Add(new PrivateLinkToken { OrderId = orderId, TokenHash = tokenHash, Purpose = PreviewPurpose, CreatedAt = now, ExpiresAt = expiresAt });
        await db.SaveChangesAsync(cancellationToken);
        return new CreatedPreviewLinkData(expiresAt);
    }

    public async Task<PreviewInvitationData?> GetPreviewAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var row = await GetAuthorizedInvitationAsync(tokenHash, now, cancellationToken);
        if (row is null) return null;
        var customizations = await GetCustomizationsAsync(row.invitation.InvitationId, cancellationToken);
        var media = await db.InvitationMedia.AsNoTracking().Where(item => item.InvitationId == row.invitation.InvitationId && item.DeletedAt == null)
            .OrderBy(item => item.SortOrder).Select(item => new InvitationMediaData(item.InvitationMediaId, item.MediaType,
                item.OriginalFileName, item.ContentType, item.SizeBytes, item.SortOrder)).ToListAsync(cancellationToken);
        var (brideName, groomName) = ParseCoupleNames(row.invitation.CoupleNames);
        var features = await (from setting in db.TemplateFeatures.AsNoTracking()
                              join feature in db.Features.AsNoTracking() on setting.FeatureId equals feature.FeatureId
                              where setting.TemplateVersionId == row.invitation.TemplateVersionId && setting.IsEnabled
                              select new InvitationFeatureData(feature.Key, feature.Name, true, setting.MaximumCount, setting.MaximumFileSizeMb))
            .ToListAsync(cancellationToken);
        return new PreviewInvitationData(row.template.Name, row.version.VersionNumber,
            ResolvePublicRendererKey(row.version.PackageKey), brideName, groomName,
            row.invitation.WeddingDateTimeLocal, row.invitation.TimeZoneId, row.invitation.VenueName,
            row.invitation.VenueAddress, row.invitation.LocationUrl, customizations, features, media);
    }

    public async Task<bool> AddReviewEventAsync(string tokenHash, DateTimeOffset now, string action, string? comment, CancellationToken cancellationToken = default)
    {
        var row = await GetAuthorizedInvitationAsync(tokenHash, now, cancellationToken);
        if (row is null) return false;
        db.PreviewReviewEvents.Add(new PreviewReviewEvent
        {
            InvitationId = row.invitation.InvitationId,
            Source = "Customer",
            Action = action,
            Comment = comment,
            CreatedAt = now
        });
        row.invitation.Status = action == "Approved" ? "Approved" : "ChangesRequested";
        row.invitation.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PreviewReviewEventData>?> GetReviewEventsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var invitationId = await db.Invitations.Where(item => item.OrderId == orderId).Select(item => (int?)item.InvitationId).FirstOrDefaultAsync(cancellationToken);
        return invitationId is null ? null : await GetEventsAsync(invitationId.Value, cancellationToken);
    }

    public async Task<InvitationMediaContentData?> GetPreviewMediaAsync(string tokenHash, int mediaId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var row = await GetAuthorizedInvitationAsync(tokenHash, now, cancellationToken);
        if (row is null) return null;
        return await db.InvitationMedia.AsNoTracking()
            .Where(item => item.InvitationId == row.invitation.InvitationId && item.InvitationMediaId == mediaId && item.DeletedAt == null)
            .Select(item => new InvitationMediaContentData(item.InvitationMediaId, item.StorageKey, item.ContentType, item.OriginalFileName))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AuthorizedInvitation?> GetAuthorizedInvitationAsync(
        string tokenHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = await (from link in db.PrivateLinkTokens.AsNoTracking()
                         join invitation in db.Invitations on link.OrderId equals invitation.OrderId
                         join order in db.Orders on link.OrderId equals order.OrderId
                         join version in db.TemplateVersions on invitation.TemplateVersionId equals version.TemplateVersionId
                         join template in db.Templates on version.TemplateId equals template.TemplateId
                         where link.TokenHash == tokenHash && link.Purpose == PreviewPurpose && link.RevokedAt == null && link.ExpiresAt > now
                             && order.DeletedAt == null
                             && invitation.Status != "Published" && invitation.Status != "Expired"
                         select new { invitation, template, version }).FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : new AuthorizedInvitation(row.invitation, row.template, row.version);
    }

    private async Task<string?> GetCustomizationsAsync(int invitationId, CancellationToken cancellationToken)
    {
        var entries = await db.InvitationCustomizations.AsNoTracking().Where(item => item.InvitationId == invitationId)
            .OrderBy(item => item.Key).ToListAsync(cancellationToken);
        if (entries.Count == 0) return null;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var entry in entries)
            {
                writer.WritePropertyName(entry.Key);
                using var value = JsonDocument.Parse(entry.ValueJson);
                value.RootElement.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static (string BrideName, string GroomName) ParseCoupleNames(string value)
    {
        try
        {
            var names = JsonSerializer.Deserialize<string[]>(value);
            if (names is { Length: 2 }) return (names[0], names[1]);
        }
        catch (JsonException) { }
        var parts = value.Split(" & ", 2);
        return (parts[0], parts.ElementAtOrDefault(1) ?? string.Empty);
    }

    private async Task<IReadOnlyList<PreviewReviewEventData>> GetEventsAsync(int invitationId, CancellationToken cancellationToken) =>
        await db.PreviewReviewEvents.AsNoTracking().Where(item => item.InvitationId == invitationId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new PreviewReviewEventData(item.PreviewReviewEventId, item.Source, item.Action,
                item.Comment, item.AdminUserId, item.CreatedAt)).ToListAsync(cancellationToken);

    private sealed record AuthorizedInvitation(Invitation invitation,
        WedLink.DataAccess.Templates.Template template,
        WedLink.DataAccess.Templates.TemplateVersion version);
}
