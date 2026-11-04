namespace WedLink.DataAccess.Invitations;

public class InvitationMedia
{
    public int InvitationMediaId { get; set; }
    public int InvitationId { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
