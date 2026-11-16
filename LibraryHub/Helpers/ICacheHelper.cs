namespace LibraryHub.Helpers;

public interface ICacheHelper
{
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken ct = default);

    // remove the cached data associated with the given key
    Task RemoveAsync(string key, CancellationToken ct = default);
}
