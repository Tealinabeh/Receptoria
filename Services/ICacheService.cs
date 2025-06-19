namespace Receptoria.API.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken token = default);
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null, CancellationToken token = default);
    Task RemoveAsync(string key, CancellationToken token = default);
}