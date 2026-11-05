namespace WedLink.Business.Invitations;

public sealed record InvitationFeatureData(string Key, string Name, bool IsEnabled, int? MaximumCount, int? MaximumFileSizeMb);
public sealed record InvitationMediaData(int MediaId, string MediaType, string OriginalFileName, string ContentType, long SizeBytes, int SortOrder);
public sealed record PreviewReviewEventData(int EventId, string Source, string Action, string? Comment, string? AdminUserId, DateTimeOffset CreatedAt);

public sealed record InvitationData(
    int OrderId, int? InvitationId, int TemplateVersionId, int? MusicTrackId, string TemplateName, string TemplateVersionNumber,
    string BrideName, string GroomName, DateTime? WeddingDateTimeLocal, string TimeZoneId,
    string? VenueName, string? VenueAddress, string? LocationUrl, string? CustomizationsJson,
    string Status, string Slug, DateTimeOffset? PublishedAt, DateTimeOffset? ExpiresAt, DateTimeOffset? PausedAt,
    DateTimeOffset? UpdatedAt, bool HasFinalPng, IReadOnlyList<InvitationFeatureData> Features,
    IReadOnlyList<InvitationMediaData> AvailableMedia, IReadOnlyList<int> SelectedMediaIds,
    IReadOnlyList<PreviewReviewEventData> ReviewEvents);

public sealed record PreviewInvitationData(
    string TemplateName, string TemplateVersionNumber, string RendererKey, string BrideName, string GroomName, DateTime? WeddingDateTimeLocal,
    string TimeZoneId, string? VenueName, string? VenueAddress, string? LocationUrl,
    string? CustomizationsJson, IReadOnlyList<InvitationFeatureData> Features, IReadOnlyList<InvitationMediaData> Media);

public sealed record PublicInvitationData(bool EventEnded, bool HasFinalPng, string TemplateName, string RendererKey, string BrideName, string GroomName,
    DateTime? WeddingDateTimeLocal, string TimeZoneId, string? VenueName, string? VenueAddress, string? LocationUrl,
    string? CustomizationsJson, IReadOnlyList<InvitationFeatureData> Features, IReadOnlyList<PublicInvitationMediaData> Media);
public sealed record PublicInvitationMediaData(int MediaId, string MediaType, string OriginalFileName, string ContentType, string StorageKey);
public sealed record PublicMediaContentData(string StorageKey, string ContentType, string OriginalFileName);
public sealed record InvitationCardData(string StorageKey, string ContentType);

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
    Task<InvitationData?> GetForAdminByInvitationIdAsync(int invitationId, CancellationToken cancellationToken = default);
    Task<InvitationSaveResult> SaveAsync(int orderId, SaveInvitationInput input, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<CreatedPreviewLinkData?> CreatePreviewLinkAsync(int orderId, string tokenHash, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
    Task<PreviewInvitationData?> GetPreviewAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<bool> AddReviewEventAsync(string tokenHash, DateTimeOffset now, string action, string? comment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PreviewReviewEventData>?> GetReviewEventsAsync(int orderId, CancellationToken cancellationToken = default);
    Task<InvitationMediaContentData?> GetPreviewMediaAsync(string tokenHash, int mediaId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<InvitationLifecycleFailure> PublishAsync(int invitationId, string slug, DateTimeOffset now, DateTimeOffset expiresAt, DateTimeOffset mediaDeleteAt, CancellationToken cancellationToken = default);
    Task<InvitationLifecycleFailure> SetPublicationStateAsync(int invitationId, string action, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<InvitationLifecycleFailure> ExtendAsync(int invitationId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<PublicInvitationData?> GetPublicAsync(string slug, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<PublicMediaContentData?> GetPublicMediaAsync(string slug, int mediaId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<(InvitationLifecycleFailure Failure, string? ReplacedStorageKey)> SetFinalPngAsync(int invitationId, string storageKey, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<InvitationCardData?> GetFinalPngAsync(int invitationId, CancellationToken cancellationToken = default);
    Task<InvitationCardData?> GetPublicFinalPngAsync(string slug, CancellationToken cancellationToken = default);
}

public interface IInvitationExpirationRepository
{
    Task<int> ExpireDueInvitationsAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}

public enum InvitationLifecycleFailure { None, NotFound, NotApproved, InvalidState, Expired, SlugConflict, MissingWeddingDate }

public sealed class InvitationNotFoundException() : Exception("الدعوة أو الطلب غير موجود.");
public sealed class PreviewLinkException() : Exception("رابط المعاينة غير صالح أو منتهي الصلاحية.");
public sealed class InvitationValidationException(string message) : Exception(message);
public sealed record PreviewLinkData(string Token, DateTimeOffset ExpiresAt);
