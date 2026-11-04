namespace WedLink.Business.Invitations;

public sealed record InvitationLifecycleSettings(int DefaultValidityMonths, int ExpirationIntervalMinutes, int MediaCleanupIntervalMinutes)
{
    public const int DefaultValidityMonthsValue = 2;
    public const int DefaultExpirationIntervalMinutes = 5;
    public const int DefaultMediaCleanupIntervalMinutes = 60;

    public void Validate()
    {
        if (DefaultValidityMonths is < 1 or > 24)
            throw new InvalidOperationException("InvitationLifecycle:DefaultValidityMonths must be between 1 and 24.");
        if (ExpirationIntervalMinutes is < 1 or > 1440)
            throw new InvalidOperationException("InvitationLifecycle:ExpirationIntervalMinutes must be between 1 and 1440.");
        if (MediaCleanupIntervalMinutes is < 1 or > 1440)
            throw new InvalidOperationException("InvitationLifecycle:MediaCleanupIntervalMinutes must be between 1 and 1440.");
    }
}
