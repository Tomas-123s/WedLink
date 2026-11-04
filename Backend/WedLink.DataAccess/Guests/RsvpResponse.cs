namespace WedLink.DataAccess.Guests;

public class RsvpResponse
{
    public int RsvpResponseId { get; set; }
    public int InvitationId { get; set; }
    public string Response { get; set; } = string.Empty;
    public string? GuestName { get; set; }
    public int AccompanyingPeople { get; set; }
    public string EditTokenHash { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
