using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WedLink.Business.Invitations;

namespace WedLink.API.Services;

public sealed class InvitationExpirationWorker(
    IServiceScopeFactory scopeFactory,
    InvitationLifecycleSettings settings,
    TimeProvider timeProvider,
    ILogger<InvitationExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.ExpirationIntervalMinutes), timeProvider);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var expiration = scope.ServiceProvider.GetRequiredService<InvitationExpirationService>();
                var expiredCount = await expiration.ExpireDueInvitationsAsync(stoppingToken);
                if (expiredCount > 0) logger.LogInformation("Expired {InvitationCount} invitation(s).", expiredCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Invitation expiration job failed; it will retry on the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
