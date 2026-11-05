using WedLink.Business.CustomerSubmissions;

namespace WedLink.Business.Guests;

public sealed class GuestService(IGuestRepository repository, IPrivateLinkTokenGenerator tokenGenerator, TimeProvider timeProvider)
{
    public async Task<GuestRsvpSubmissionData> SubmitRsvpAsync(string slug, string? editToken, GuestRsvpInput input, CancellationToken cancellationToken = default)
    {
        var context = await repository.GetPublicContextAsync(slug, timeProvider.GetUtcNow(), cancellationToken)
            ?? throw new GuestInvitationNotFoundException();
        if (!context.RsvpEnabled) throw new GuestValidationException("تأكيد الحضور غير متاح لهذه الدعوة.");
        if (input.Response is not ("Yes" or "No")) throw new GuestValidationException("اختر نعم أو لا لتأكيد الحضور.");
        var guestName = Clean(input.GuestName);
        if (guestName?.Length > 150) throw new GuestValidationException("الاسم يتجاوز 150 حرفًا.");
        if (input.AccompanyingPeople < 0)
            throw new GuestValidationException("عدد المرافقين يجب أن يكون رقمًا موجبًا صالحًا.");
        var isUpdate = !string.IsNullOrWhiteSpace(editToken);
        string rawToken;
        string hash;
        if (isUpdate)
        {
            if (editToken!.Length > 100) throw new GuestValidationException("رمز تعديل الرد غير صالح.");
            rawToken = editToken;
            hash = tokenGenerator.Hash(editToken);
        }
        else (rawToken, hash) = tokenGenerator.Generate();
        var saved = await repository.SaveRsvpAsync(context.InvitationId, hash, isUpdate,
            input with { GuestName = guestName, AccompanyingPeople = input.Response == "Yes" ? input.AccompanyingPeople : 0 },
            timeProvider.GetUtcNow(), cancellationToken);
        if (saved is null) throw new GuestValidationException("رمز تعديل الرد غير صالح.");
        return new GuestRsvpSubmissionData(saved, rawToken, isUpdate);
    }

    public async Task<GuestInteractionData> GetPublicInteractionsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var context = await repository.GetPublicContextAsync(slug, timeProvider.GetUtcNow(), cancellationToken)
            ?? throw new GuestInvitationNotFoundException();
        var messages = context.MessagesEnabled
            ? await repository.GetPublicMessagesAsync(context.InvitationId, cancellationToken)
            : Array.Empty<GuestMessageData>();
        var expected = context.GuestCountEnabled
            ? await repository.GetExpectedAttendanceAsync(context.InvitationId, cancellationToken) : 0;
        var selectedMusic = context.MusicEnabled
            ? await repository.GetInvitationMusicAsync(slug, timeProvider.GetUtcNow(), cancellationToken) : null;
        return new GuestInteractionData(context.RsvpEnabled, context.MaximumAccompanyingPeople,
            context.GuestCountEnabled, expected, context.MessagesEnabled, messages,
            context.MusicEnabled, selectedMusic?.Track, selectedMusic is null ? null : $"/api/v1/public/invitations/{Uri.EscapeDataString(slug)}/music");
    }

    public async Task<GuestMessageData> AddMessageAsync(string slug, GuestMessageInput input, CancellationToken cancellationToken = default)
    {
        var context = await repository.GetPublicContextAsync(slug, timeProvider.GetUtcNow(), cancellationToken)
            ?? throw new GuestInvitationNotFoundException();
        if (!context.MessagesEnabled) throw new GuestValidationException("رسائل التهنئة غير متاحة لهذه الدعوة.");
        var name = Clean(input.GuestName);
        var message = Clean(input.Message);
        if (name?.Length > 150) throw new GuestValidationException("الاسم يتجاوز 150 حرفًا.");
        if (message is null || message.Length > 2000) throw new GuestValidationException("اكتب رسالة لا تتجاوز 2000 حرف.");
        return await repository.AddMessageAsync(context.InvitationId, new GuestMessageInput(name, message), timeProvider.GetUtcNow(), cancellationToken)
            ?? throw new GuestInvitationNotFoundException();
    }

    public Task<GuestRsvpSummaryData?> GetRsvpsForAdminAsync(int invitationId, CancellationToken cancellationToken = default) =>
        repository.GetRsvpsForAdminAsync(invitationId, cancellationToken);
    public Task<IReadOnlyList<AdminGuestMessageData>?> GetMessagesForAdminAsync(int invitationId, CancellationToken cancellationToken = default) =>
        repository.GetMessagesForAdminAsync(invitationId, cancellationToken);
    public Task<bool> DeleteMessageAsync(int invitationId, int messageId, string adminUserId, CancellationToken cancellationToken = default) =>
        repository.DeleteMessageAsync(invitationId, messageId, adminUserId, timeProvider.GetUtcNow(), cancellationToken);
    public Task<bool> RestoreMessageAsync(int invitationId, int messageId, CancellationToken cancellationToken = default) =>
        repository.RestoreMessageAsync(invitationId, messageId, cancellationToken);
    public Task<IReadOnlyList<MusicTrackData>> GetMusicTracksAsync(bool activeOnly, CancellationToken cancellationToken = default) =>
        repository.GetMusicTracksAsync(activeOnly, cancellationToken);
    public Task<MusicTrackData> AddMusicTrackAsync(string title, string? artist, string rightsReference, string storageKey, string contentType, CancellationToken cancellationToken = default) =>
        repository.AddMusicTrackAsync(title.Trim(), Clean(artist), rightsReference.Trim(), storageKey, contentType, timeProvider.GetUtcNow(), cancellationToken);
    public Task<MusicTrackData?> UpdateMusicTrackAsync(int id, string title, string? artist, string rights, bool active, CancellationToken cancellationToken = default) =>
        repository.UpdateMusicTrackAsync(id, title.Trim(), Clean(artist), rights.Trim(), active, cancellationToken);
    public Task<bool> SetInvitationMusicAsync(int invitationId, int? musicTrackId, CancellationToken cancellationToken = default) =>
        repository.SetInvitationMusicAsync(invitationId, musicTrackId, cancellationToken);
    public Task<(MusicTrackData Track, string StorageKey)?> GetInvitationMusicAsync(string slug, CancellationToken cancellationToken = default) =>
        repository.GetInvitationMusicAsync(slug, timeProvider.GetUtcNow(), cancellationToken);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
