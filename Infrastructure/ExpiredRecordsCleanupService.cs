using PruebaIdempotencia.Domain.Idempotency;

namespace PruebaIdempotencia.Infrastructure;

public sealed class ExpiredRecordsCleanupService(IIdempotencyStore store, ILogger<ExpiredRecordsCleanupService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_interval);
        try
        {
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                var removed = await store.CleanupExpiredAsync(DateTimeOffset.UtcNow, stoppingToken);
                if (removed > 0)
                {
                    logger.LogInformation("Idempotency cleanup removed {Count} expired records", removed);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal during shutdown
        }
    }
}
