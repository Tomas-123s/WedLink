using WedLink.Business.Invitations;

namespace WedLink.Tests.Invitations;

public sealed class InvitationLifecycleSettingsTests
{
    [Fact]
    public void DefaultsAreValidAndOutOfRangeSettingsAreRejected()
    {
        new InvitationLifecycleSettings(2, 5, 60).Validate();
        Assert.Throws<InvalidOperationException>(() => new InvitationLifecycleSettings(0, 5, 60).Validate());
        Assert.Throws<InvalidOperationException>(() => new InvitationLifecycleSettings(2, 1441, 60).Validate());
        Assert.Throws<InvalidOperationException>(() => new InvitationLifecycleSettings(2, 5, 0).Validate());
    }
}
