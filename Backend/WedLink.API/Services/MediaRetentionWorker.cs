using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WedLink.Business.Invitations;

namespace WedLink.API.Services;

public sealed class MediaRetentionWorker(
    IServiceScopeFactory scopeFactory,
    InvitationLifecycleSettings settings,
    TimeProvider timeProvider,
    ILogger<MediaRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.MediaCleanupIntervalMinutes), timeProvider);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var retention = scope.ServiceProvider.GetRequiredService<MediaRetentionService>();
                var result = await retention.CleanDueMediaAsync(stoppingToken);
                if (result.FilesFailed > 0)
                    logger.LogWarning("Media retention run deleted {FileCount} file(s), marked {MetadataCount} row(s), but failed to delete {FailureCount} file(s); failed items will retry.",
                        result.FilesDeleted, result.MetadataRowsMarkedDeleted, result.FilesFailed);
                else if (result.FilesDeleted > 0)
                    logger.LogInformation("Media retention run: deleted {FileCount} file(s), marked {MetadataCount} row(s), failed {FailureCount} file(s).",
                        result.FilesDeleted, result.MetadataRowsMarkedDeleted, result.FilesFailed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Media retention job failed; it will retry on the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
