using System.Text.Json;
using System.Security.Cryptography;
using WedLink.Business.CustomerSubmissions;

namespace WedLink.Business.Invitations;

public sealed class InvitationService(IInvitationRepository repository, IPrivateLinkTokenGenerator tokenGenerator,
    TimeProvider timeProvider, InvitationLifecycleSettings lifecycleSettings, ISubmissionFileStorage? storage = null)
{
    private static readonly TimeSpan PreviewLifetime = TimeSpan.FromDays(14);

    public async Task<InvitationData> GetForAdminAsync(int orderId, CancellationToken cancellationToken = default) =>
        await repository.GetForAdminAsync(orderId, cancellationToken) ?? throw new InvitationNotFoundException();

    public async Task<InvitationData> SaveAsync(int orderId, SaveInvitationInput input, CancellationToken cancellationToken = default)
    {
        var bride = input.BrideName?.Trim() ?? string.Empty;
        var groom = input.GroomName?.Trim() ?? string.Empty;
        if (bride.Length is < 1 or > 150 || groom.Length is < 1 or > 150)
            throw new InvitationValidationException("أدخل اسمي العروسين بحد أقصى 150 حرفًا لكل اسم.");
        var details = await repository.GetForAdminAsync(orderId, cancellationToken) ?? throw new InvitationNotFoundException();
        var zoneId = string.IsNullOrWhiteSpace(input.TimeZoneId) ? "Africa/Cairo" : input.TimeZoneId.Trim();
        if (input.WeddingDateTimeLocal is null || input.WeddingDateTimeLocal.Value.Kind == DateTimeKind.Utc)
            throw new InvitationValidationException("أدخل موعد الزفاف بالتوقيت المحلي.");
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
            if (zone.IsInvalidTime(input.WeddingDateTimeLocal.Value) || zone.IsAmbiguousTime(input.WeddingDateTimeLocal.Value))
                throw new InvitationValidationException("موعد الزفاف غير صالح للمنطقة الزمنية المختارة.");
            var keepingPublishedWeddingDate = (details.Status is "Published" or "Paused")
                && details.WeddingDateTimeLocal == input.WeddingDateTimeLocal;
            if (!keepingPublishedWeddingDate
                && input.WeddingDateTimeLocal.Value <= TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), zone).DateTime)
                throw new InvitationValidationException("موعد الزفاف يجب أن يكون في المستقبل.");
        }
        catch (TimeZoneNotFoundException) { throw new InvitationValidationException("المنطقة الزمنية غير مدعومة."); }
        catch (InvalidTimeZoneException) { throw new InvitationValidationException("المنطقة الزمنية غير صالحة."); }

        var customizationJson = NormalizeCustomizations(input.CustomizationsJson, details.Features);
        var selectedMedia = details.AvailableMedia.Where(item => input.MediaIds.Contains(item.MediaId)).ToArray();
        if (selectedMedia.Length != input.MediaIds.Distinct().Count())
            throw new InvitationValidationException("تتضمن الملفات المختارة ملفًا غير متاح لهذا الطلب.");
        foreach (var group in selectedMedia.GroupBy(item => item.MediaType switch
                 { "Image" => "gallery", "Video" => "video", "Audio" => "music", _ => "" }))
        {
            var feature = details.Features.FirstOrDefault(item => item.Key == group.Key && item.IsEnabled);
            if (feature is null) throw new InvitationValidationException("القالب لا يدعم أحد أنواع الملفات المختارة.");
            if (feature.MaximumCount is int count && group.Count() > count)
                throw new InvitationValidationException($"عدد الملفات المختارة يتجاوز الحد المسموح ({count}).");
            if (feature.MaximumFileSizeMb is int maxMb && group.Any(item => item.SizeBytes > maxMb * 1024L * 1024L))
                throw new InvitationValidationException($"حجم أحد الملفات المختارة يتجاوز حد القالب ({maxMb}MB).");
        }
        ValidateLength(input.VenueName, 200, "اسم المكان");
        ValidateLength(input.VenueAddress, 500, "عنوان المكان");
        ValidateLength(input.LocationUrl, 1000, "رابط الموقع");
        var location = string.IsNullOrWhiteSpace(input.LocationUrl) ? null : input.LocationUrl.Trim();
        if (location is not null && (!Uri.TryCreate(location, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http")))
            throw new InvitationValidationException("رابط الموقع يجب أن يبدأ بـ https:// أو http://.");

        var saveResult = await repository.SaveAsync(orderId, input with
        {
            BrideName = bride, GroomName = groom, TimeZoneId = zoneId,
            VenueName = Clean(input.VenueName), VenueAddress = Clean(input.VenueAddress),
            LocationUrl = location, CustomizationsJson = customizationJson,
            MediaIds = input.MediaIds.Distinct().ToArray()
        }, timeProvider.GetUtcNow(), cancellationToken);
        var savedInvitation = saveResult.Failure switch
        {
            InvitationSaveFailure.None => saveResult.Invitation!,
            InvitationSaveFailure.OrderNotFound => throw new InvitationNotFoundException(),
            InvitationSaveFailure.InvitationLocked => throw new InvitationValidationException("انتهت صلاحية الدعوة؛ لا يمكن تعديل وسائطها أو محتواها."),
            InvitationSaveFailure.MediaUnavailable => throw new InvitationValidationException("تتضمن الملفات المختارة ملفًا غير متاح لهذا الطلب."),
            InvitationSaveFailure.UnsupportedFeature => throw new InvitationValidationException("القالب لا يدعم أحد أنواع الملفات المختارة."),
            _ => throw new InvalidOperationException("Unknown invitation save failure.")
        };
        if (saveResult.ReplacedFinalPngStorageKey is not null && storage is not null)
            await storage.DeleteAsync(saveResult.ReplacedFinalPngStorageKey, cancellationToken);
        return savedInvitation;
    }

    public async Task<PreviewLinkData> CreatePreviewLinkAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var (token, hash) = tokenGenerator.Generate();
        var now = timeProvider.GetUtcNow();
        var result = await repository.CreatePreviewLinkAsync(orderId, hash, now, now.Add(PreviewLifetime), cancellationToken)
            ?? throw new InvitationNotFoundException();
        return new PreviewLinkData(token, result.ExpiresAt);
    }

    public async Task<PreviewInvitationData> GetPreviewAsync(string token, CancellationToken cancellationToken = default) =>
        await repository.GetPreviewAsync(HashToken(token), timeProvider.GetUtcNow(), cancellationToken) ?? throw new PreviewLinkException();

    public async Task RecordReviewAsync(string token, string action, string? comment, CancellationToken cancellationToken = default)
    {
        if (action is not ("Approved" or "ChangesRequested")) throw new InvitationValidationException("نوع المراجعة غير صالح.");
        var cleanComment = Clean(comment);
        if (cleanComment?.Length > 2000) throw new InvitationValidationException("ملاحظة المراجعة تتجاوز 2000 حرف.");
        if (action == "ChangesRequested" && cleanComment is null) throw new InvitationValidationException("اكتب التعديلات المطلوبة.");
        if (!await repository.AddReviewEventAsync(HashToken(token), timeProvider.GetUtcNow(), action, cleanComment, cancellationToken))
            throw new PreviewLinkException();
    }

    public async Task<IReadOnlyList<PreviewReviewEventData>> GetReviewEventsAsync(int orderId, CancellationToken cancellationToken = default) =>
        await repository.GetReviewEventsAsync(orderId, cancellationToken) ?? throw new InvitationNotFoundException();

    public async Task PublishAsync(int invitationId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var invitation = await repository.GetForAdminByInvitationIdAsync(invitationId, cancellationToken)
            ?? throw new InvitationNotFoundException();
        if (invitation.WeddingDateTimeLocal is null) throw new InvitationValidationException("أدخل موعد الزفاف قبل النشر.");
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(invitation.TimeZoneId); }
        catch (TimeZoneNotFoundException) { throw new InvitationValidationException("المنطقة الزمنية للدعوة غير مدعومة."); }
        catch (InvalidTimeZoneException) { throw new InvitationValidationException("المنطقة الزمنية للدعوة غير صالحة."); }
        var wedding = invitation.WeddingDateTimeLocal.Value;
        DateTimeOffset expiresAt;
        DateTimeOffset mediaDeleteAt;
        try
        {
            expiresAt = WeddingTimePolicy.AddCalendarMonths(wedding, invitation.TimeZoneId, lifecycleSettings.DefaultValidityMonths);
            mediaDeleteAt = WeddingTimePolicy.AddCalendarMonths(wedding, invitation.TimeZoneId, 3);
        }
        catch (InvalidTimeZoneException) { throw new InvitationValidationException("تعذر حساب تواريخ الدعوة للمنطقة الزمنية المحددة."); }
        if (expiresAt <= now) throw new InvitationValidationException("انتهت مدة صلاحية الدعوة قبل نشرها.");
        var slug = Convert.ToHexString(RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();
        ThrowLifecycleFailure(await repository.PublishAsync(invitationId, slug, now, expiresAt, mediaDeleteAt, cancellationToken));
    }

    public async Task PauseAsync(int invitationId, CancellationToken cancellationToken = default) =>
        ThrowLifecycleFailure(await repository.SetPublicationStateAsync(invitationId, "Pause", timeProvider.GetUtcNow(), cancellationToken));
    public async Task ReactivateAsync(int invitationId, CancellationToken cancellationToken = default) =>
        ThrowLifecycleFailure(await repository.SetPublicationStateAsync(invitationId, "Reactivate", timeProvider.GetUtcNow(), cancellationToken));
    public async Task ExtendAsync(int invitationId, CancellationToken cancellationToken = default) =>
        ThrowLifecycleFailure(await repository.ExtendAsync(invitationId, timeProvider.GetUtcNow(), cancellationToken));

    public async Task<PublicInvitationData> GetPublicAsync(string slug, CancellationToken cancellationToken = default) =>
        await repository.GetPublicAsync(slug, timeProvider.GetUtcNow(), cancellationToken)
            ?? throw new InvitationNotFoundException();

    public Task<PublicMediaContentData?> GetPublicMediaAsync(string slug, int mediaId, CancellationToken cancellationToken = default) =>
        repository.GetPublicMediaAsync(slug, mediaId, timeProvider.GetUtcNow(), cancellationToken);

    public Task<InvitationCardData?> GetFinalPngAsync(int invitationId, CancellationToken cancellationToken = default) =>
        repository.GetFinalPngAsync(invitationId, cancellationToken);
    public Task<InvitationCardData?> GetPublicFinalPngAsync(string slug, CancellationToken cancellationToken = default) =>
        repository.GetPublicFinalPngAsync(slug, cancellationToken);

    public async Task<string?> SetFinalPngAsync(int invitationId, string storageKey, CancellationToken cancellationToken = default)
    {
        var result = await repository.SetFinalPngAsync(invitationId, storageKey, timeProvider.GetUtcNow(), cancellationToken);
        ThrowLifecycleFailure(result.Failure);
        return result.ReplacedStorageKey;
    }

    private static void ThrowLifecycleFailure(InvitationLifecycleFailure failure)
    {
        switch (failure)
        {
            case InvitationLifecycleFailure.None: return;
            case InvitationLifecycleFailure.NotFound: throw new InvitationNotFoundException();
            case InvitationLifecycleFailure.NotApproved: throw new InvitationValidationException("لا يمكن نشر الدعوة قبل موافقة العميل.");
            case InvitationLifecycleFailure.MissingWeddingDate: throw new InvitationValidationException("موعد الزفاف مطلوب للنشر.");
            case InvitationLifecycleFailure.Expired: throw new InvitationValidationException("انتهت صلاحية الدعوة ولا يمكن إعادة تفعيلها.");
            case InvitationLifecycleFailure.SlugConflict: throw new InvitationValidationException("تعذر إنشاء رابط فريد للدعوة؛ حاول النشر مرة أخرى.");
            default: throw new InvitationValidationException("حالة الدعوة لا تسمح بهذا الإجراء.");
        }
    }

    public async Task<InvitationMediaContentData?> GetPreviewMediaAsync(string token, int mediaId, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(token);
        var now = timeProvider.GetUtcNow();
        if (await repository.GetPreviewAsync(hash, now, cancellationToken) is null) throw new PreviewLinkException();
        return await repository.GetPreviewMediaAsync(hash, mediaId, now, cancellationToken);
    }

    private string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) throw new PreviewLinkException();
        return tokenGenerator.Hash(token);
    }

    private static string? NormalizeCustomizations(string? json, IReadOnlyList<InvitationFeatureData> features)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        if (json.Length > 20000) throw new InvitationValidationException("بيانات التخصيص طويلة جدًا.");
        try
        {
            using var document = JsonDocument.Parse(json);
            var allowed = features.Where(item => item.IsEnabled).Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
            if (document.RootElement.ValueKind != JsonValueKind.Object || document.RootElement.EnumerateObject().Any(item => !allowed.Contains(item.Name)))
                throw new InvitationValidationException("تتضمن التخصيصات خيارًا غير متاح في القالب المحدد.");
            return document.RootElement.GetRawText();
        }
        catch (JsonException) { throw new InvitationValidationException("بيانات التخصيص غير صالحة."); }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void ValidateLength(string? value, int max, string label)
    {
        if (value?.Length > max) throw new InvitationValidationException($"{label} يتجاوز الحد المسموح ({max} حرفًا).");
    }
}
