namespace WedLink.Business.Invitations;

public sealed record MediaRetentionBatch(int OrderId, int? InvitationId, IReadOnlyList<string> StorageKeys);
public sealed record MediaRetentionRunData(int FilesDeleted, int MetadataRowsMarkedDeleted, int FilesFailed);

public interface IMediaRetentionRepository
{
    Task<IReadOnlyList<MediaRetentionBatch>> GetDueMediaBatchesAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken = default);
    Task<int> MarkMediaDeletedAsync(int orderId, int? invitationId, IReadOnlyList<string> storageKeys, DateTimeOffset now, CancellationToken cancellationToken = default);
}
