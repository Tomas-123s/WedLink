namespace WedLink.Business.CustomerSubmissions;

public sealed record SubmissionLinkData(int LinkId, string Token, DateTimeOffset ExpiresAt);
public sealed record CurrentSubmissionLinkData(int LinkId, DateTimeOffset ExpiresAt);

public sealed record WeddingSubmissionData(
    int OrderId, string CustomerName, string BrideName, string GroomName,
    DateTime? WeddingDateTimeLocal, string TimeZoneId, string? VenueName,
    string? VenueAddress, string? LocationUrl, string? CustomizationsJson,
    DateTimeOffset? SubmittedAt, DateTimeOffset? AdminReviewedAt, IReadOnlyList<SubmissionMediaData> Media,
    IReadOnlyList<SubmissionFeatureData> AvailableFeatures);

public sealed record SubmissionFeatureData(string Key, string Name, string? Description, int? MaximumCount, int? MaximumFileSizeMb);
public sealed record SubmissionMediaFeatureLimit(int? MaximumCount, int? MaximumFileSizeMb);

public sealed record SubmissionMediaData(
    int MediaId, int OrderId, string OriginalFileName, string ContentType,
    long SizeBytes, string MediaKind, DateTimeOffset UploadedAt);

public sealed record SaveWeddingSubmissionInput(
    string BrideName, string GroomName, DateTime? WeddingDateTimeLocal,
    string TimeZoneId, string? VenueName, string? VenueAddress,
    string? LocationUrl, string? CustomizationsJson, bool Submit);

public sealed record SubmissionLinkCreatedData(int LinkId, DateTimeOffset ExpiresAt);

public interface ICustomerSubmissionRepository
{
    Task<SubmissionLinkCreatedData?> CreateLinkAsync(int orderId, string tokenHash, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
    Task<CurrentSubmissionLinkData?> GetCurrentLinkAsync(int orderId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<bool> RevokeLinkAsync(int orderId, int linkId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<int?> GetOrderIdForTokenAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<WeddingSubmissionData?> GetSubmissionAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<WeddingSubmissionData?> SaveSubmissionAsync(string tokenHash, DateTimeOffset now, SaveWeddingSubmissionInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>?> GetAllowedCustomizationKeysAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<long?> GetActiveMediaBytesAsync(int orderId, CancellationToken cancellationToken = default);
    Task<int> GetActiveMediaCountAsync(int orderId, string mediaKind, CancellationToken cancellationToken = default);
    Task<SubmissionMediaFeatureLimit?> GetMediaFeatureLimitAsync(int orderId, string featureKey, CancellationToken cancellationToken = default);
    Task<SubmissionMediaData?> AddMediaAsync(int orderId, string originalFileName, string contentType, long sizeBytes, string storageKey, string mediaKind, DateTimeOffset uploadedAt, CancellationToken cancellationToken = default);
    Task<(SubmissionMediaData? Media, string? StorageKey)?> GetMediaAsync(int orderId, int mediaId, CancellationToken cancellationToken = default);
    Task<(SubmissionMediaData? Media, string? StorageKey)?> DeleteMediaAsync(int orderId, int mediaId, string? adminUserId, DateTimeOffset deletedAt, CancellationToken cancellationToken = default);
    Task<WeddingSubmissionData?> GetForAdminAsync(int orderId, CancellationToken cancellationToken = default);
    Task<bool> MarkReviewedAsync(int orderId, string adminId, DateTimeOffset reviewedAt, CancellationToken cancellationToken = default);
}

public interface IPrivateLinkTokenGenerator
{
    (string Token, string Hash) Generate();
    string Hash(string token);
}

public interface ISubmissionFileStorage
{
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

public sealed class SubmissionValidationException(string message) : Exception(message);
public sealed class SubmissionLinkException() : Exception("The submission link is invalid or has expired.");
public sealed class SubmissionNotFoundException() : Exception("The requested submission was not found.");
