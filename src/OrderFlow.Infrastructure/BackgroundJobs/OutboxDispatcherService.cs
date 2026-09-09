namespace OrderFlow.Infrastructure.BackgroundJobs;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
using OrderFlow.Infrastructure.Persistence;

public sealed class OutboxDispatcherService(
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                var pending = await db.OutboxMessages
                    .Where(m => m.ProcessedOnUtc == null)
                    .OrderBy(m => m.OccurredOnUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var message in pending)
                {
                    //logger.LogInformation("Outbox ready to dispatch: {Type} ({Id})", message.Type, message.Id);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                }

                if (pending.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) { /* shutting down */ }
            catch (Exception )
            {
                //logger.LogError(ex, "Outbox dispatch loop failed; will retry next tick.");
            }
        }
    }
}
