namespace OrderFlow.Application.UnitTests.TestDoubles;

using OrderFlow.Application.Common.Interfaces;

/// <summary>Records commits and transaction scopes without touching a database.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public int TransactionCalls { get; private set; }

    /// <summary>True while the delegate passed to <see cref="ExecuteInTransactionAsync"/> is running.</summary>
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
