using WedLink.Business.CustomerSubmissions;

namespace WedLink.Business.Invitations;

public sealed class MediaRetentionService(IMediaRetentionRepository repository,
    ISubmissionFileStorage storage, TimeProvider timeProvider)
{
    private const int BatchSize = 50;

    public async Task<MediaRetentionRunData> CleanDueMediaAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var batches = await repository.GetDueMediaBatchesAsync(now, BatchSize, cancellationToken);
        var filesDeleted = 0;
        var rowsDeleted = 0;
        var filesFailed = 0;
        foreach (var batch in batches)
        {
            var successfulKeys = new List<string>();
            foreach (var key in batch.StorageKeys.Distinct(StringComparer.Ordinal))
            {
                try
                {
                    await storage.DeleteAsync(key, cancellationToken);
                    successfulKeys.Add(key);
                    filesDeleted++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception) { filesFailed++; }
            }
            if (successfulKeys.Count > 0)
                rowsDeleted += await repository.MarkMediaDeletedAsync(batch.OrderId, batch.InvitationId,
                    successfulKeys, now, cancellationToken);
        }
        return new MediaRetentionRunData(filesDeleted, rowsDeleted, filesFailed);
    }
}
