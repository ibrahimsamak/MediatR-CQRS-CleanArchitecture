namespace OrderFlow.Application.UnitTests.TestDoubles;

using OrderFlow.Application.Common.Interfaces;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public int TransactionCalls { get; private set; }

    public bool InTransaction { get; private set; }

    public bool SavedInsideTransaction { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCalls++;
        if (InTransaction)
        {
            SavedInsideTransaction = true;
        }

        return Task.FromResult(1);
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        TransactionCalls++;
        InTransaction = true;
        try
        {
            await action();
        }
        finally
        {
            InTransaction = false;
        }
    }
}
