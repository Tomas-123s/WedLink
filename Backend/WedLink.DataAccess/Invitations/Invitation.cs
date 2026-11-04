namespace WedLink.DataAccess.Invitations;

public class Invitation
{
    public int InvitationId { get; set; }
    public int OrderId { get; set; }
    public int TemplateVersionId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTime? WeddingDateTimeLocal { get; set; }
    public string TimeZoneId { get; set; } = "Africa/Cairo";
    public string CoupleNames { get; set; } = string.Empty;
    public string? VenueName { get; set; }
    public string? VenueAddress { get; set; }
    public string? LocationUrl { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? MediaDeleteAt { get; set; }
    public DateTimeOffset? PausedAt { get; set; }
    public string? FinalPngStorageKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
