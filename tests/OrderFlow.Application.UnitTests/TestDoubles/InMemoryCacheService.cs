namespace OrderFlow.Application.UnitTests.TestDoubles;

using OrderFlow.Application.Common.Interfaces;

/// <summary>Dictionary-backed <see cref="ICacheService"/> with the same cache-aside contract as the Redis one.</summary>
internal sealed class InMemoryCacheService : ICacheService
{
    private readonly Dictionary<string, object?> _entries = [];

    public List<string> Keys => [.. _entries.Keys];

    public int FactoryInvocations { get; private set; }

    public TimeSpan? LastTtl { get; private set; }

    public void Preload<T>(string key, T value) => _entries[key] = value;

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) =>
        Task.FromResult(_entries.TryGetValue(key, out var value) ? (T?)value : default);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        _entries[key] = value;
        LastTtl = ttl;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _entries.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (_entries.TryGetValue(key, out var cached))
        {
            return (T)cached!;
        }

        FactoryInvocations++;
        var created = await factory(ct);
        await SetAsync(key, created, ttl, ct);
        return created;
    }
}
