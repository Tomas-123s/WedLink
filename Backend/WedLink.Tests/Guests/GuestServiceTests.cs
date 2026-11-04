using WedLink.Business.CustomerSubmissions;
using WedLink.Business.Guests;

namespace WedLink.Tests.Guests;

public sealed class GuestServiceTests
{
    [Fact]
    public async Task SubmitRsvpNormalizesGuestDataAndIssuesOpaqueEditToken()
    {
        var repository = new FakeGuestRepository { Context = new GuestInvitationContext(7, true, 3, true, true, false) };
        var service = new GuestService(repository, new FakeTokenGenerator(), TimeProvider.System);

        var result = await service.SubmitRsvpAsync("public-slug", null, new GuestRsvpInput("Yes", "  Sara  ", 2));

        Assert.False(result.IsUpdate);
        Assert.Equal("opaque-edit-token", result.EditToken);
        Assert.Equal("Sara", repository.Saved!.GuestName);
        Assert.Equal(2, repository.Saved.AccompanyingPeople);
        Assert.DoesNotContain("opaque-edit-token", repository.SavedHash);
    }

    [Fact]
    public async Task SubmitRsvpRejectsDisabledRsvpInvalidResponseAndExcessGuests()
    {
        var repository = new FakeGuestRepository { Context = new GuestInvitationContext(7, false, 1, true, true, false) };
        var service = new GuestService(repository, new FakeTokenGenerator(), TimeProvider.System);
        await Assert.ThrowsAsync<GuestValidationException>(() => service.SubmitRsvpAsync("slug", null, new("Yes", null, 0)));

        repository.Context = repository.Context with { RsvpEnabled = true };
        await Assert.ThrowsAsync<GuestValidationException>(() => service.SubmitRsvpAsync("slug", null, new("Maybe", null, 0)));
        await Assert.ThrowsAsync<GuestValidationException>(() => service.SubmitRsvpAsync("slug", null, new("Yes", null, 2)));
    }

    private sealed class FakeTokenGenerator : IPrivateLinkTokenGenerator
    {
        public (string Token, string Hash) Generate() => ("opaque-edit-token", "stored-hash");
        public string Hash(string token) => "stored-hash";
    }

    private sealed class FakeGuestRepository : IGuestRepository
    {
        public GuestInvitationContext Context { get; set; } = new(7, true, null, true, true, false);
        public GuestRsvpInput? Saved { get; private set; }
        public string? SavedHash { get; private set; }
        public Task<GuestInvitationContext?> GetPublicContextAsync(string slug, DateTimeOffset now, CancellationToken cancellationToken = default) => Task.FromResult<GuestInvitationContext?>(Context);
        public Task<IReadOnlyList<GuestMessageData>> GetPublicMessagesAsync(int invitationId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GuestMessageData>>([]);
        public Task<long> GetExpectedAttendanceAsync(int invitationId, CancellationToken cancellationToken = default) => Task.FromResult(0L);
        public Task<GuestRsvpData?> SaveRsvpAsync(int id, string hash, bool update, GuestRsvpInput input, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            Saved = input; SavedHash = hash;
            return Task.FromResult<GuestRsvpData?>(new(1, input.Response, input.GuestName, input.AccompanyingPeople, 3, now, null));
        }
        public Task<GuestRsvpSummaryData?> GetRsvpsForAdminAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<GuestRsvpSummaryData?>(null);
        public Task<GuestMessageData?> AddMessageAsync(int id, GuestMessageInput input, DateTimeOffset now, CancellationToken cancellationToken = default) => Task.FromResult<GuestMessageData?>(null);
        public Task<IReadOnlyList<AdminGuestMessageData>?> GetMessagesForAdminAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdminGuestMessageData>?>([]);
        public Task<bool> DeleteMessageAsync(int invitationId, int messageId, string adminUserId, DateTimeOffset now, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<MusicTrackData>> GetMusicTracksAsync(bool activeOnly, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<MusicTrackData>>([]);
        public Task<MusicTrackData> AddMusicTrackAsync(string title, string? artist, string rightsReference, string storageKey, string contentType, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<MusicTrackData?> UpdateMusicTrackAsync(int musicTrackId, string title, string? artist, string rightsReference, bool isActive, CancellationToken cancellationToken = default) => Task.FromResult<MusicTrackData?>(null);
        public Task<bool> SetInvitationMusicAsync(int invitationId, int? musicTrackId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<(MusicTrackData Track, string StorageKey)?> GetInvitationMusicAsync(string slug, DateTimeOffset now, CancellationToken cancellationToken = default) => Task.FromResult<(MusicTrackData Track, string StorageKey)?>(null);
    }
}
