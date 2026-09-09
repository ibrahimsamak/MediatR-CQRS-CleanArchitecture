namespace OrderFlow.Infrastructure.Caching;

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using OrderFlow.Application.Common.Interfaces;

public sealed class RedisCacheService(IDistributedCache cache) : ICacheService
{
    // Per-key in-process lock => single-flight, prevents a cache stampede on miss.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) =>
        cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(value),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);

    public Task RemoveAsync(string key, CancellationToken ct = default) => cache.RemoveAsync(key, ct);

    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var gate = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            cached = await GetAsync<T>(key, ct);      // double-check after acquiring the lock
            if (cached is not null) return cached;

            var value = await factory(ct);
            await SetAsync(key, value, ttl, ct);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }
}
