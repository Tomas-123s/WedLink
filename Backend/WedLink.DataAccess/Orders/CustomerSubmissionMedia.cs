namespace WedLink.DataAccess.Orders;

public class CustomerSubmissionMedia
{
    public int CustomerSubmissionMediaId { get; set; }
    public int OrderId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string MediaKind { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByAdminUserId { get; set; }
}
