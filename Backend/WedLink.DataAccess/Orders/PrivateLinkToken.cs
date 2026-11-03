namespace WedLink.DataAccess.Orders;

public class PrivateLinkToken
{
    public int PrivateLinkTokenId { get; set; }
    public int OrderId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string Purpose { get; set; } = "Submission";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
