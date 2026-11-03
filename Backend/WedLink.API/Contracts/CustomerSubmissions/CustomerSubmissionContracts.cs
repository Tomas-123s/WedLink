using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using WedLink.Business.CustomerSubmissions;

namespace WedLink.API.Contracts.CustomerSubmissions;

public sealed record SaveWeddingSubmissionRequest(
    [Required, StringLength(150, MinimumLength = 1)] string BrideName,
    [Required, StringLength(150, MinimumLength = 1)] string GroomName,
    DateTime? WeddingDateTimeLocal,
    [StringLength(100)] string? TimeZoneId,
    [StringLength(200)] string? VenueName,
    [StringLength(500)] string? VenueAddress,
    [StringLength(1000)] string? LocationUrl,
    JsonElement? Customizations,
    bool Submit = true)
{
    public SaveWeddingSubmissionInput ToInput() => new(
        BrideName, GroomName, WeddingDateTimeLocal,
        string.IsNullOrWhiteSpace(TimeZoneId) ? "Africa/Cairo" : TimeZoneId,
        VenueName, VenueAddress, LocationUrl,
        Customizations?.GetRawText(), Submit);
}

public sealed record SubmissionLinkResponse(int LinkId, string SubmissionUrl, DateTimeOffset ExpiresAt);

public sealed record SubmissionMediaResponse(
    int MediaId, int OrderId, string OriginalFileName, string ContentType,
    long SizeBytes, string MediaKind, DateTimeOffset UploadedAt)
{
    public static SubmissionMediaResponse FromData(SubmissionMediaData item) =>
        new(item.MediaId, item.OrderId, item.OriginalFileName, item.ContentType,
            item.SizeBytes, item.MediaKind, item.UploadedAt);
}

public sealed record WeddingSubmissionResponse(
    int OrderId, string CustomerName, string BrideName, string GroomName,
    DateTime? WeddingDateTimeLocal, string TimeZoneId, string? VenueName,
    string? VenueAddress, string? LocationUrl, JsonElement? Customizations,
    DateTimeOffset? SubmittedAt, DateTimeOffset? AdminReviewedAt, IReadOnlyList<SubmissionMediaResponse> Media,
    IReadOnlyList<SubmissionFeatureData> AvailableFeatures)
{
    public static WeddingSubmissionResponse FromData(WeddingSubmissionData item)
    {
        JsonElement? customization = null;
        if (!string.IsNullOrWhiteSpace(item.CustomizationsJson))
        {
            using var document = JsonDocument.Parse(item.CustomizationsJson);
            customization = document.RootElement.Clone();
        }
        return new(item.OrderId, item.CustomerName, item.BrideName, item.GroomName,
            item.WeddingDateTimeLocal, item.TimeZoneId, item.VenueName,
            item.VenueAddress, item.LocationUrl, customization, item.SubmittedAt, item.AdminReviewedAt,
            item.Media.Select(SubmissionMediaResponse.FromData).ToList(), item.AvailableFeatures);
    }
}
