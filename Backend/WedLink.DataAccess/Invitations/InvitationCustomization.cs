namespace WedLink.DataAccess.Invitations;

public class InvitationCustomization
{
    public int InvitationCustomizationId { get; set; }
    public int InvitationId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string ValueJson { get; set; } = string.Empty;
}
