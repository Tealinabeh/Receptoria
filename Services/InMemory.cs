
using Microsoft.Extensions.Caching.Memory;

namespace Receptoria.API.Services;

public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;

    public InMemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        _memoryCache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null, CancellationToken token = default)
    {
        _memoryCache.Set(key, value, absoluteExpireTime ?? TimeSpan.FromMinutes(60));
        return Task.CompletedTask;
    }
}