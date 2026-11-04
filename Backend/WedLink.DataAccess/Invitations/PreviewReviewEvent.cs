namespace WedLink.DataAccess.Invitations;

public class PreviewReviewEvent
{
    public int PreviewReviewEventId { get; set; }
    public int InvitationId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? AdminUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
