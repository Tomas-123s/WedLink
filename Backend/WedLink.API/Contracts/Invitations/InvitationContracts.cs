using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using WedLink.Business.Invitations;

namespace WedLink.API.Contracts.Invitations;

public sealed record SaveInvitationRequest(
    [Required, StringLength(150, MinimumLength = 1)] string BrideName,
    [Required, StringLength(150, MinimumLength = 1)] string GroomName,
    DateTime? WeddingDateTimeLocal,
    [StringLength(100)] string? TimeZoneId,
    [StringLength(200)] string? VenueName,
    [StringLength(500)] string? VenueAddress,
    [StringLength(1000)] string? LocationUrl,
    JsonElement? Customizations,
    IReadOnlyList<int>? MediaIds)
{
    public SaveInvitationInput ToInput() => new(BrideName, GroomName, WeddingDateTimeLocal,
        string.IsNullOrWhiteSpace(TimeZoneId) ? "Africa/Cairo" : TimeZoneId,
        VenueName, VenueAddress, LocationUrl, Customizations?.GetRawText(), MediaIds ?? []);
}

public sealed record InvitationFeatureResponse(string Key, string Name, bool IsEnabled, int? MaximumCount, int? MaximumFileSizeMb);
public sealed record InvitationMediaResponse(int MediaId, string MediaType, string OriginalFileName, string ContentType, long SizeBytes, int SortOrder);
public sealed record PreviewReviewEventResponse(int EventId, string Source, string Action, string? Comment, string? AdminUserId, DateTimeOffset CreatedAt);

public sealed record InvitationResponse(
    int OrderId, int? InvitationId, int TemplateVersionId, string TemplateName, string TemplateVersionNumber,
    string BrideName, string GroomName, DateTime? WeddingDateTimeLocal, string TimeZoneId,
    string? VenueName, string? VenueAddress, string? LocationUrl, JsonElement? Customizations,
    string Status, DateTimeOffset? UpdatedAt, IReadOnlyList<InvitationFeatureResponse> Features,
    IReadOnlyList<InvitationMediaResponse> AvailableMedia, IReadOnlyList<int> SelectedMediaIds,
    IReadOnlyList<PreviewReviewEventResponse> ReviewEvents)
{
    public static InvitationResponse FromData(InvitationData item) => new(item.OrderId, item.InvitationId,
        item.TemplateVersionId, item.TemplateName, item.TemplateVersionNumber, item.BrideName, item.GroomName,
        item.WeddingDateTimeLocal, item.TimeZoneId, item.VenueName, item.VenueAddress, item.LocationUrl,
        Parse(item.CustomizationsJson), item.Status, item.UpdatedAt,
        item.Features.Select(feature => new InvitationFeatureResponse(feature.Key, feature.Name, feature.IsEnabled, feature.MaximumCount, feature.MaximumFileSizeMb)).ToArray(),
        item.AvailableMedia.Select(InvitationContractMapping.FromData).ToArray(), item.SelectedMediaIds,
        item.ReviewEvents.Select(InvitationContractMapping.FromData).ToArray());

    private static JsonElement? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}

public sealed record PreviewInvitationResponse(string TemplateName, string TemplateVersionNumber, string BrideName, string GroomName,
    DateTime? WeddingDateTimeLocal, string TimeZoneId, string? VenueName, string? VenueAddress, string? LocationUrl,
    JsonElement? Customizations, IReadOnlyList<InvitationFeatureResponse> Features, IReadOnlyList<InvitationMediaResponse> Media)
{
    public static PreviewInvitationResponse FromData(PreviewInvitationData item)
    {
        JsonElement? customizations = null;
        if (!string.IsNullOrWhiteSpace(item.CustomizationsJson))
        {
            using var document = JsonDocument.Parse(item.CustomizationsJson);
            customizations = document.RootElement.Clone();
        }
        return new(item.TemplateName, item.TemplateVersionNumber, item.BrideName, item.GroomName, item.WeddingDateTimeLocal,
            item.TimeZoneId, item.VenueName, item.VenueAddress, item.LocationUrl, customizations,
            item.Features.Select(feature => new InvitationFeatureResponse(feature.Key, feature.Name, feature.IsEnabled, feature.MaximumCount, feature.MaximumFileSizeMb)).ToArray(),
            item.Media.Select(InvitationContractMapping.FromData).ToArray());
    }
}

public sealed record CreatePreviewLinkResponse(string PreviewUrl, DateTimeOffset ExpiresAt);
public sealed record PreviewReviewRequest([Required] string Action, [StringLength(2000)] string? Comment);

internal static class InvitationContractMapping
{
    public static InvitationMediaResponse FromData(InvitationMediaData item) =>
        new(item.MediaId, item.MediaType, item.OriginalFileName, item.ContentType, item.SizeBytes, item.SortOrder);
    public static PreviewReviewEventResponse FromData(PreviewReviewEventData item) =>
        new(item.EventId, item.Source, item.Action, item.Comment, item.AdminUserId, item.CreatedAt);
}
