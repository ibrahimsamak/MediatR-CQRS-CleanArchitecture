using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Persistence;

public sealed class UnitOfWork(OrderDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
    {
        var strategy = db.Database.CreateExecutionStrategy(); // retries on transient SQL errors
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await action();
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }
}