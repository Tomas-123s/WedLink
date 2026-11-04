namespace WedLink.Business.Invitations;

public sealed record InvitationFeatureData(string Key, string Name, bool IsEnabled, int? MaximumCount, int? MaximumFileSizeMb);
public sealed record InvitationMediaData(int MediaId, string MediaType, string OriginalFileName, string ContentType, long SizeBytes, int SortOrder);
public sealed record PreviewReviewEventData(int EventId, string Source, string Action, string? Comment, string? AdminUserId, DateTimeOffset CreatedAt);

public sealed record InvitationData(
    int OrderId, int? InvitationId, int TemplateVersionId, string TemplateName, string TemplateVersionNumber,
    string BrideName, string GroomName, DateTime? WeddingDateTimeLocal, string TimeZoneId,
    string? VenueName, string? VenueAddress, string? LocationUrl, string? CustomizationsJson,
    string Status, DateTimeOffset? UpdatedAt, IReadOnlyList<InvitationFeatureData> Features,
    IReadOnlyList<InvitationMediaData> AvailableMedia, IReadOnlyList<int> SelectedMediaIds,
    IReadOnlyList<PreviewReviewEventData> ReviewEvents);

public sealed record PreviewInvitationData(
    string TemplateName, string TemplateVersionNumber, string BrideName, string GroomName, DateTime? WeddingDateTimeLocal,
    string TimeZoneId, string? VenueName, string? VenueAddress, string? LocationUrl,
    string? CustomizationsJson, IReadOnlyList<InvitationFeatureData> Features, IReadOnlyList<InvitationMediaData> Media);

public sealed record CreatedPreviewLinkData(DateTimeOffset ExpiresAt);
public sealed record InvitationMediaContentData(int MediaId, string StorageKey, string ContentType, string OriginalFileName);
public sealed record SaveInvitationInput(string BrideName, string GroomName, DateTime? WeddingDateTimeLocal,
    string TimeZoneId, string? VenueName, string? VenueAddress, string? LocationUrl,
    string? CustomizationsJson, IReadOnlyList<int> MediaIds);

public enum InvitationSaveFailure { None, OrderNotFound, InvitationLocked, MediaUnavailable, UnsupportedFeature }
public sealed record InvitationSaveResult(InvitationData? Invitation, InvitationSaveFailure Failure);

public interface IInvitationRepository
{
    Task<InvitationData?> GetForAdminAsync(int orderId, CancellationToken cancellationToken = default);
    Task<InvitationSaveResult> SaveAsync(int orderId, SaveInvitationInput input, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<CreatedPreviewLinkData?> CreatePreviewLinkAsync(int orderId, string tokenHash, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
    Task<PreviewInvitationData?> GetPreviewAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<bool> AddReviewEventAsync(string tokenHash, DateTimeOffset now, string action, string? comment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PreviewReviewEventData>?> GetReviewEventsAsync(int orderId, CancellationToken cancellationToken = default);
    Task<InvitationMediaContentData?> GetPreviewMediaAsync(string tokenHash, int mediaId, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed class InvitationNotFoundException() : Exception("الدعوة أو الطلب غير موجود.");
public sealed class PreviewLinkException() : Exception("رابط المعاينة غير صالح أو منتهي الصلاحية.");
public sealed class InvitationValidationException(string message) : Exception(message);
public sealed record PreviewLinkData(string Token, DateTimeOffset ExpiresAt);
