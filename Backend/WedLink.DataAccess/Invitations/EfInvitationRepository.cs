using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WedLink.Business.Invitations;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Persistence;

namespace WedLink.DataAccess.Invitations;

public sealed class EfInvitationRepository(WedLinkDbContext db) : IInvitationRepository
{
    private const string PreviewPurpose = "InvitationPreview";

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
        var selectedStorageKeys = row.invitation is null
            ? Array.Empty<string>()
            : await db.InvitationMedia.AsNoTracking().Where(item => item.InvitationId == row.invitation.InvitationId && item.DeletedAt == null)
                .Select(item => item.StorageKey).ToArrayAsync(cancellationToken);
        var selectedMediaIds = await db.CustomerSubmissionMedia.AsNoTracking()
            .Where(item => item.OrderId == orderId && item.DeletedAt == null && selectedStorageKeys.Contains(item.StorageKey))
            .Select(item => item.CustomerSubmissionMediaId).ToArrayAsync(cancellationToken);
        var reviewEvents = row.invitation is null
            ? Array.Empty<PreviewReviewEventData>()
            : (await GetEventsAsync(row.invitation.InvitationId, cancellationToken)).ToArray();

        var (brideName, groomName) = row.invitation is null
            ? (row.submission?.BrideName ?? "", row.submission?.GroomName ?? "")
            : ParseCoupleNames(row.invitation.CoupleNames);
        return new InvitationData(orderId, row.invitation?.InvitationId, row.SelectedTemplateVersionId, row.Name,
            row.VersionNumber, brideName, groomName,
            row.invitation?.WeddingDateTimeLocal ?? row.submission?.WeddingDateTimeLocal,
            row.invitation?.TimeZoneId ?? row.submission?.TimeZoneId ?? "Africa/Cairo",
            row.invitation?.VenueName ?? row.submission?.VenueName, row.invitation?.VenueAddress ?? row.submission?.VenueAddress,
            row.invitation?.LocationUrl ?? row.submission?.LocationUrl, customizations,
            row.invitation?.Status ?? "Draft", row.invitation?.UpdatedAt, features, availableMedia, selectedMediaIds, reviewEvents);
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
        else if (invitation.Status == "Published" || invitation.Status == "Expired")
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

        invitation.CoupleNames = JsonSerializer.Serialize(new[] { input.BrideName, input.GroomName },
            new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        invitation.WeddingDateTimeLocal = input.WeddingDateTimeLocal;
        invitation.TimeZoneId = input.TimeZoneId;
        invitation.VenueName = input.VenueName;
        invitation.VenueAddress = input.VenueAddress;
        invitation.LocationUrl = input.LocationUrl;
        invitation.Status = "Draft";
        invitation.UpdatedAt = now;

        var existingCustomizations = await db.InvitationCustomizations.Where(item => item.InvitationId == invitation.InvitationId).ToListAsync(cancellationToken);
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

        var invitationMedia = await db.InvitationMedia.Where(item => item.InvitationId == invitation.InvitationId && item.DeletedAt == null).ToListAsync(cancellationToken);
        foreach (var existing in invitationMedia)
        {
            var sourceStillSelected = allowedMedia.Values.FirstOrDefault(item => item.StorageKey == existing.StorageKey);
            if (sourceStillSelected is null) existing.DeletedAt = now;
        }
        var existingKeys = invitationMedia.Select(item => item.StorageKey).ToHashSet(StringComparer.Ordinal);
        var nextSortOrder = invitationMedia.Count == 0 ? 0 : invitationMedia.Max(item => item.SortOrder) + 1;
        foreach (var media in allowedMedia.Values.Where(item => !existingKeys.Contains(item.StorageKey)))
            db.InvitationMedia.Add(new InvitationMedia
            {
                InvitationId = invitation.InvitationId,
                MediaType = media.MediaKind,
                OriginalFileName = media.OriginalFileName,
                ContentType = media.ContentType,
                SizeBytes = media.SizeBytes,
                StorageKey = media.StorageKey,
                SortOrder = nextSortOrder++,
                UploadedAt = now
            });
        await db.SaveChangesAsync(cancellationToken);
        return new(await GetForAdminAsync(orderId, cancellationToken), InvitationSaveFailure.None);
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
        return new PreviewInvitationData(row.template.Name, row.version.VersionNumber, brideName, groomName,
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
