using WedLink.Business.CustomerSubmissions;
using WedLink.Business.Invitations;

namespace WedLink.Tests.Invitations;

public sealed class InvitationLifecycleServiceTests
{
    [Fact]
    public async Task PublishUsesCalendarValidityAndMediaRetentionDates()
    {
        var now = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
        var repository = new FakeInvitationRepository
        {
            Invitation = Invitation(new DateTime(2027, 1, 31, 18, 0, 0))
        };
        var service = new InvitationService(repository, new PrivateLinkTokenGenerator(),
            new FixedTimeProvider(now), new InvitationLifecycleSettings(2, 5, 60));

        await service.PublishAsync(8);

        Assert.Equal(new DateTimeOffset(2027, 3, 31, 16, 0, 0, TimeSpan.Zero), repository.Expiry);
        Assert.Equal(new DateTimeOffset(2027, 4, 30, 15, 0, 0, TimeSpan.Zero), repository.MediaDeleteAt);
        Assert.Equal("Published", repository.StatusAction);
        Assert.Equal(20, repository.Slug!.Length);
    }

    [Fact]
    public async Task PublishRejectsMissingWeddingDateAndUnapprovedInvitation()
    {
        var repo = new FakeInvitationRepository { Invitation = Invitation(null) };
        var service = new InvitationService(repo, new PrivateLinkTokenGenerator(),
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero)), new(2, 5, 60));
        await Assert.ThrowsAsync<InvitationValidationException>(() => service.PublishAsync(8));

        repo.Invitation = Invitation(new DateTime(2027, 1, 31, 18, 0, 0));
        repo.PublishFailure = InvitationLifecycleFailure.NotApproved;
        await Assert.ThrowsAsync<InvitationValidationException>(() => service.PublishAsync(8));
    }

    private static InvitationData Invitation(DateTime? weddingDate) => new(
        42, 8, 3, null, "Classic", "1", "default", "Bride", "Groom", weddingDate, "Africa/Cairo", null, null, null, null,
        "Draft", "", null, null, null, null, false, [], [], [], []);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeInvitationRepository : IInvitationRepository
    {
        public InvitationData? Invitation { get; set; }
        public DateTimeOffset? Expiry { get; private set; }
        public DateTimeOffset? MediaDeleteAt { get; private set; }
        public string? Slug { get; private set; }
        public string? StatusAction { get; private set; }
        public InvitationLifecycleFailure PublishFailure { get; set; }
        public Task<InvitationData?> GetForAdminByInvitationIdAsync(int id, CancellationToken ct = default) => Task.FromResult(Invitation);
        public Task<InvitationLifecycleFailure> PublishAsync(int id, string slug, DateTimeOffset now, DateTimeOffset expiresAt, DateTimeOffset mediaDeleteAt, CancellationToken ct = default)
        { Slug = slug; Expiry = expiresAt; MediaDeleteAt = mediaDeleteAt; StatusAction = "Published"; return Task.FromResult(PublishFailure); }
        public Task<InvitationData?> GetForAdminAsync(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<InvitationSaveResult> SaveAsync(int id, SaveInvitationInput input, DateTimeOffset now, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<CreatedPreviewLinkData?> CreatePreviewLinkAsync(int id, string hash, DateTimeOffset now, DateTimeOffset expires, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<PreviewInvitationData?> GetPreviewAsync(string hash, DateTimeOffset now, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> AddReviewEventAsync(string hash, DateTimeOffset now, string action, string? comment, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<PreviewReviewEventData>?> GetReviewEventsAsync(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<InvitationMediaContentData?> GetPreviewMediaAsync(string hash, int id, DateTimeOffset now, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<InvitationLifecycleFailure> SetPublicationStateAsync(int id, string action, DateTimeOffset now, CancellationToken ct = default) { StatusAction = action; return Task.FromResult(InvitationLifecycleFailure.None); }
        public Task<InvitationLifecycleFailure> ExtendAsync(int id, DateTimeOffset now, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<PublicInvitationData?> GetPublicAsync(string slug, DateTimeOffset now, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<PublicMediaContentData?> GetPublicMediaAsync(string slug, int id, DateTimeOffset now, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(InvitationLifecycleFailure Failure, string? ReplacedStorageKey)> SetFinalPngAsync(int id, string key, DateTimeOffset now, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<InvitationCardData?> GetFinalPngAsync(int id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<InvitationCardData?> GetPublicFinalPngAsync(string slug, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
