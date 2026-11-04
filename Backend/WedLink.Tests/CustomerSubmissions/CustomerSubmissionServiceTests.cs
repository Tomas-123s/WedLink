using WedLink.Business.CustomerSubmissions;

namespace WedLink.Tests.CustomerSubmissions;

public sealed class CustomerSubmissionServiceTests
{
    [Fact]
    public async Task PrivateLinkReturnsRawCredentialOnceAndStoresOnlyItsHash()
    {
        var repository = new FakeRepository();
        var generator = new PrivateLinkTokenGenerator();
        var service = new CustomerSubmissionService(repository, generator);

        var created = await service.CreateLinkAsync(42);
        Assert.Equal(43, created.Token.Length);
        Assert.Equal(generator.Hash(created.Token), repository.SavedTokenHash);
        Assert.InRange(created.ExpiresAt - DateTimeOffset.UtcNow, TimeSpan.FromDays(13.99), TimeSpan.FromDays(14.01));

        await service.GetCurrentAsync(created.Token);
        Assert.Equal(generator.Hash(created.Token), repository.LookupHash);
        Assert.DoesNotContain(created.Token, repository.SavedTokenHash);
    }

    [Fact]
    public async Task InvalidPrivateTokenIsRejectedBeforeRepositoryLookup()
    {
        var repository = new FakeRepository();
        var service = new CustomerSubmissionService(repository, new PrivateLinkTokenGenerator());

        await Assert.ThrowsAsync<SubmissionLinkException>(() => service.GetCurrentAsync(new string('x', 101)));
        Assert.Null(repository.LookupHash);
    }

    private sealed class FakeRepository : ICustomerSubmissionRepository
    {
        public string? SavedTokenHash { get; private set; }
        public string? LookupHash { get; private set; }
        public Task<SubmissionLinkCreatedData?> CreateLinkAsync(int orderId, string tokenHash, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        { SavedTokenHash = tokenHash; return Task.FromResult<SubmissionLinkCreatedData?>(new(1, expiresAt)); }
        public Task<WeddingSubmissionData?> GetSubmissionAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
        { LookupHash = tokenHash; return Task.FromResult<WeddingSubmissionData?>(new(42, "Customer", "Bride", "Groom", null, "Africa/Cairo", null, null, null, null, null, null, [], [])); }
        public Task<CurrentSubmissionLinkData?> GetCurrentLinkAsync(int orderId, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> RevokeLinkAsync(int orderId, int linkId, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int?> GetOrderIdForTokenAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<WeddingSubmissionData?> SaveSubmissionAsync(string tokenHash, DateTimeOffset now, SaveWeddingSubmissionInput input, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<string>?> GetAllowedCustomizationKeysAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<long?> GetActiveMediaBytesAsync(int orderId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetActiveMediaCountAsync(int orderId, string mediaKind, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SubmissionMediaFeatureLimit?> GetMediaFeatureLimitAsync(int orderId, string featureKey, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SubmissionMediaData?> AddMediaAsync(int orderId, string originalFileName, string contentType, long sizeBytes, string storageKey, string mediaKind, DateTimeOffset uploadedAt, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(SubmissionMediaData? Media, string? StorageKey)?> GetMediaAsync(int orderId, int mediaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(SubmissionMediaData? Media, string? StorageKey)?> DeleteMediaAsync(int orderId, int mediaId, string? adminUserId, DateTimeOffset deletedAt, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<WeddingSubmissionData?> GetForAdminAsync(int orderId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> MarkReviewedAsync(int orderId, string adminId, DateTimeOffset reviewedAt, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
