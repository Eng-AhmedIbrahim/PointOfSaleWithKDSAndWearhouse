namespace POS.Core.Services.Contract;

public interface IFlexibleCacheService<T>
{
    Task SetAsync(string key, T value, TimeSpan? expiration);
    Task<T?> GetAsync(string key);
    Task RemoveAsync(string key);
    Task ClearAsync();
}
