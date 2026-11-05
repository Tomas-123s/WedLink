using System.Text.Json;

namespace WedLink.Business.CustomerSubmissions;

public sealed class CustomerSubmissionService(
    ICustomerSubmissionRepository repository,
    IPrivateLinkTokenGenerator tokenGenerator)
{
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromDays(14);

    public async Task<SubmissionLinkData> CreateLinkAsync(int orderId, CancellationToken cancellationToken = default)
    {
        if (orderId < 1) throw new SubmissionNotFoundException();
        var (token, hash) = tokenGenerator.Generate();
        var now = DateTimeOffset.UtcNow;
        var created = await repository.CreateLinkAsync(orderId, hash, now, now.Add(LinkLifetime), cancellationToken)
            ?? throw new SubmissionNotFoundException();
        return new SubmissionLinkData(created.LinkId, token, created.ExpiresAt);
    }

    public async Task RevokeLinkAsync(int orderId, int linkId, CancellationToken cancellationToken = default)
    {
        if (!await repository.RevokeLinkAsync(orderId, linkId, DateTimeOffset.UtcNow, cancellationToken))
            throw new SubmissionNotFoundException();
    }

    public Task<CurrentSubmissionLinkData?> GetCurrentLinkAsync(int orderId, CancellationToken cancellationToken = default) =>
        repository.GetCurrentLinkAsync(orderId, DateTimeOffset.UtcNow, cancellationToken);

    public async Task<WeddingSubmissionData> GetCurrentAsync(string token, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(token);
        return await repository.GetSubmissionAsync(hash, DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new SubmissionLinkException();
    }

    public async Task<WeddingSubmissionData> SaveCurrentAsync(
        string token, SaveWeddingSubmissionInput input, CancellationToken cancellationToken = default)
    {
        var bride = Clean(input.BrideName);
        var groom = Clean(input.GroomName);
        var timezone = Clean(input.TimeZoneId) ?? "Africa/Cairo";
        if (bride.Length > 150 || groom.Length > 150 || bride.Length == 0 || groom.Length == 0)
            throw new SubmissionValidationException("أدخل اسمي العروسين بحد أقصى 150 حرفًا لكل اسم.");
        if (input.WeddingDateTimeLocal is null)
            throw new SubmissionValidationException("أدخل موعد الزفاف.");
        if (input.WeddingDateTimeLocal.Value.Kind == DateTimeKind.Utc)
            throw new SubmissionValidationException("أدخل موعد الزفاف بالتوقيت المحلي.");
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(timezone); }
        catch (TimeZoneNotFoundException) { throw new SubmissionValidationException("المنطقة الزمنية غير مدعومة."); }
        catch (InvalidTimeZoneException) { throw new SubmissionValidationException("المنطقة الزمنية غير صالحة."); }
        if (zone.IsInvalidTime(input.WeddingDateTimeLocal.Value))
            throw new SubmissionValidationException("موعد الزفاف غير صالح بسبب تغيير التوقيت الصيفي.");
        if (zone.IsAmbiguousTime(input.WeddingDateTimeLocal.Value))
            throw new SubmissionValidationException("وقت الزفاف المحدد يتكرر بسبب تغيير التوقيت الصيفي؛ اختر وقتًا آخر.");
        if (input.WeddingDateTimeLocal.Value <= TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime)
            throw new SubmissionValidationException("موعد الزفاف يجب أن يكون في المستقبل.");

        var allowed = await repository.GetAllowedCustomizationKeysAsync(HashToken(token), DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new SubmissionLinkException();
        var customizations = Clean(input.CustomizationsJson);
        if (customizations is not null)
        {
            try
            {
                using var document = JsonDocument.Parse(customizations);
                if (document.RootElement.ValueKind != JsonValueKind.Object
                    || document.RootElement.EnumerateObject().Any(item => !allowed.Contains(item.Name, StringComparer.Ordinal)))
                    throw new SubmissionValidationException("تتضمن التخصيصات خيارًا غير متاح في القالب المحدد.");
            }
            catch (JsonException) { throw new SubmissionValidationException("بيانات التخصيص غير صالحة."); }
            if (customizations.Length > 20000) throw new SubmissionValidationException("بيانات التخصيص طويلة جدًا.");
        }

        var normalized = input with
        {
            BrideName = bride,
            GroomName = groom,
            TimeZoneId = timezone,
            VenueName = TrimOptional(input.VenueName),
            VenueAddress = TrimOptional(input.VenueAddress),
            LocationUrl = ValidateLocation(input.LocationUrl),
            CustomizationsJson = customizations
        };
        ValidateLength(normalized.VenueName, 200, "اسم المكان");
        ValidateLength(normalized.VenueAddress, 500, "عنوان المكان");
        ValidateLength(normalized.LocationUrl, 1000, "رابط الموقع");
        return await repository.SaveSubmissionAsync(HashToken(token), DateTimeOffset.UtcNow, normalized, cancellationToken)
            ?? throw new SubmissionLinkException();
    }

    public async Task<SubmissionMediaData> UploadAsync(
        string token, string filename, string contentType, long size, Stream content,
        ISubmissionFileStorage storage, CancellationToken cancellationToken = default)
    {
        var orderId = await repository.GetOrderIdForTokenAsync(HashToken(token), DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new SubmissionLinkException();
        return await UploadForOrderAsync(orderId, filename, contentType, size, content, storage, cancellationToken, enforceMediaCount: true);
    }

    public Task<SubmissionMediaData> UploadForAdminAsync(
        int orderId, string filename, string contentType, long size, Stream content,
        ISubmissionFileStorage storage, CancellationToken cancellationToken = default)
    {
        if (orderId < 1) throw new SubmissionNotFoundException();
        return UploadForOrderAsync(orderId, filename, contentType, size, content, storage, cancellationToken, enforceMediaCount: false);
    }

    private async Task<SubmissionMediaData> UploadForOrderAsync(
        int orderId, string filename, string contentType, long size, Stream content,
        ISubmissionFileStorage storage, CancellationToken cancellationToken, bool enforceMediaCount)
    {
        var media = MediaFilePolicy.Validate(filename, contentType, size, content);
        var featureKey = media.Kind switch { "Image" => "gallery", "Video" => "video", "Audio" => "music", _ => "" };
        var featureLimit = await repository.GetMediaFeatureLimitAsync(orderId, featureKey, cancellationToken);
        if (featureLimit is null) throw new SubmissionValidationException("هذا النوع من الملفات غير متاح في القالب المختار.");
        if (featureLimit.MaximumFileSizeMb is int maximumMb && size > maximumMb * 1024L * 1024L)
            throw new SubmissionValidationException($"حجم الملف يتجاوز حد القالب ({maximumMb}MB).");
        if (enforceMediaCount && featureLimit.MaximumCount is int maximumCount
            && await repository.GetActiveMediaCountAsync(orderId, media.Kind, cancellationToken) >= maximumCount)
            throw new SubmissionValidationException($"وصلت إلى الحد الأقصى لعدد ملفات {media.Kind} في القالب.");
        var usedBytes = await repository.GetActiveMediaBytesAsync(orderId, cancellationToken) ?? 0;
        if (usedBytes + size > 500L * 1024 * 1024)
            throw new SubmissionValidationException("تجاوزت الملفات الحد الإجمالي المسموح به للطلب (500MB).");
        var key = await storage.SaveAsync(content, cancellationToken);
        try
        {
            return await repository.AddMediaAsync(orderId, Path.GetFileName(filename), media.ContentType,
                size, key, media.Kind, DateTimeOffset.UtcNow, cancellationToken)
                ?? throw new SubmissionLinkException();
        }
        catch
        {
            await storage.DeleteAsync(key, cancellationToken);
            throw;
        }
    }

    public Task<(SubmissionMediaData? Media, string? StorageKey)?> GetMediaAsync(int orderId, int mediaId, CancellationToken cancellationToken = default) =>
        repository.GetMediaAsync(orderId, mediaId, cancellationToken);

    public async Task DeleteCustomerMediaAsync(string token, int mediaId, ISubmissionFileStorage storage, CancellationToken cancellationToken = default)
    {
        var orderId = await repository.GetOrderIdForTokenAsync(HashToken(token), DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new SubmissionLinkException();
        var item = await repository.DeleteMediaAsync(orderId, mediaId, null, DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new SubmissionNotFoundException();
        if (item.StorageKey is not null) await storage.DeleteAsync(item.StorageKey, cancellationToken);
    }

    public async Task DeleteAdminMediaAsync(int orderId, int mediaId, string adminId, ISubmissionFileStorage storage, CancellationToken cancellationToken = default)
    {
        var item = await repository.DeleteMediaAsync(orderId, mediaId, adminId, DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new SubmissionNotFoundException();
        if (item.StorageKey is not null) await storage.DeleteAsync(item.StorageKey, cancellationToken);
    }

    public async Task<WeddingSubmissionData> GetForAdminAsync(int orderId, CancellationToken cancellationToken = default) =>
        await repository.GetForAdminAsync(orderId, cancellationToken) ?? throw new SubmissionNotFoundException();

    public async Task MarkReviewedAsync(int orderId, string adminId, CancellationToken cancellationToken = default)
    {
        if (!await repository.MarkReviewedAsync(orderId, adminId, DateTimeOffset.UtcNow, cancellationToken))
            throw new SubmissionNotFoundException();
    }

    private string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) throw new SubmissionLinkException();
        return tokenGenerator.Hash(token);
    }

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;
    private static string? TrimOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void ValidateLength(string? value, int max, string label)
    {
        if (value?.Length > max) throw new SubmissionValidationException($"{label} يتجاوز الحد المسموح ({max} حرفًا).");
    }
    private static string? ValidateLocation(string? input)
    {
        var value = TrimOptional(input);
        if (value is null) return null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new SubmissionValidationException("رابط الموقع يجب أن يبدأ بـ https:// أو http://.");
        return value;
    }
}

public static class MediaFilePolicy
{
    private sealed record Rule(string Kind, string ContentType, long MaxBytes, string[] Extensions);
    private static readonly Dictionary<string, Rule> Rules = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new("Image", "image/jpeg", 10L * 1024 * 1024, [".jpg", ".jpeg"]),
        [".jpeg"] = new("Image", "image/jpeg", 10L * 1024 * 1024, [".jpg", ".jpeg"]),
        [".png"] = new("Image", "image/png", 10L * 1024 * 1024, [".png"]),
        [".webp"] = new("Image", "image/webp", 10L * 1024 * 1024, [".webp"]),
        [".mp4"] = new("Video", "video/mp4", 100L * 1024 * 1024, [".mp4"]),
        [".mp3"] = new("Audio", "audio/mpeg", 20L * 1024 * 1024, [".mp3"]),
        [".m4a"] = new("Audio", "audio/mp4", 20L * 1024 * 1024, [".m4a"])
    };

    public static (string Kind, string ContentType) Validate(string filename, string contentType, long size, Stream stream)
    {
        var extension = Path.GetExtension(Path.GetFileName(filename));
        if (!Rules.TryGetValue(extension, out var rule)) throw new SubmissionValidationException("نوع الملف غير مسموح.");
        if (size <= 0 || size > rule.MaxBytes) throw new SubmissionValidationException($"حجم الملف يتجاوز الحد المسموح لهذا النوع ({rule.MaxBytes / 1024 / 1024}MB).");
        if (!string.Equals(contentType, rule.ContentType, StringComparison.OrdinalIgnoreCase))
            throw new SubmissionValidationException("نوع محتوى الملف لا يطابق امتداده.");
        Span<byte> header = stackalloc byte[16];
        var read = stream.Read(header);
        if (stream.CanSeek) stream.Position = 0;
        var valid = extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => read >= 8 && header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".webp" => read >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            ".mp4" or ".m4a" => read >= 12 && header[4..8].SequenceEqual("ftyp"u8),
            ".mp3" => read >= 3 && (header[..3].SequenceEqual("ID3"u8) || (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)),
            _ => false
        };
        if (!valid) throw new SubmissionValidationException("تعذر التحقق من محتوى الملف.");
        return (rule.Kind, rule.ContentType);
    }
}
