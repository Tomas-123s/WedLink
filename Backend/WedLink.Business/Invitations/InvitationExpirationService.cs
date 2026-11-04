namespace WedLink.Business.Invitations;

public sealed class InvitationExpirationService(IInvitationExpirationRepository repository, TimeProvider timeProvider)
{
    public Task<int> ExpireDueInvitationsAsync(CancellationToken cancellationToken = default) =>
        repository.ExpireDueInvitationsAsync(timeProvider.GetUtcNow(), cancellationToken);
}
