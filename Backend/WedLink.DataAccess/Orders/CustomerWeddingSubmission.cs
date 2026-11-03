namespace WedLink.DataAccess.Orders;

public class CustomerWeddingSubmission
{
    public int CustomerWeddingSubmissionId { get; set; }
    public int OrderId { get; set; }
    public string BrideName { get; set; } = string.Empty;
    public string GroomName { get; set; } = string.Empty;
    public DateTime? WeddingDateTimeLocal { get; set; }
    public string TimeZoneId { get; set; } = "Africa/Cairo";
    public string? VenueName { get; set; }
    public string? VenueAddress { get; set; }
    public string? LocationUrl { get; set; }
    public string? CustomizationsJson { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? AdminReviewedAt { get; set; }
    public string? AdminReviewedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
