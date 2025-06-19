using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace Receptoria.API.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _serializerOptions;

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
        _serializerOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        var jsonData = await _cache.GetStringAsync(key, token);
        return jsonData is null ? default : JsonSerializer.Deserialize<T>(jsonData, _serializerOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null, CancellationToken token = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? TimeSpan.FromMinutes(60)
        };
        var jsonData = JsonSerializer.Serialize(value, _serializerOptions);
        await _cache.SetStringAsync(key, jsonData, options, token);
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        await _cache.RemoveAsync(key, token);
    }
}