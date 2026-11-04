namespace WedLink.DataAccess.Guests;

public class GuestMessage
{
    public int GuestMessageId { get; set; }
    public int InvitationId { get; set; }
    public string? GuestName { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByAdminUserId { get; set; }
}
