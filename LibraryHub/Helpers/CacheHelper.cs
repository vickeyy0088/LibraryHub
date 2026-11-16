using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LibraryHub.Helpers
{
    public class CacheHelper(
        IDistributedCache cache,
        ILogger<CacheHelper> logger) : ICacheHelper
    {
        // Saves the injected Redis cache service into a private read-only field
        private readonly IDistributedCache _cache = cache;
        // Saves the injected logger into a private read-only field so errors can be logged if Redis fails.
        private readonly ILogger<CacheHelper> _logger = logger;
        // enables camelCase property names, case-insensitive matching, and reading numbers from strings.
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<T?> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T?>> factory,
            TimeSpan? absoluteExpiration = null,
            TimeSpan? slidingExpiration = null,
            CancellationToken ct = default)
        {
            try
            {
                // try to retrieve the value from redis
                string? json = await _cache.GetStringAsync(key, ct);

                // return the cached value if it exists
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonSerializer.Deserialize<T>(json, JsonOptions);
                }
            }
            catch (Exception ex)
            {
                // log the error and continue to fetch the value from the factory
                _logger.LogWarning(ex, "Redis GET failed for key {CacheKey}. Falling back to DB.", key);
            }

            // cache iss: get the value from the db or another source
            T? value = await factory(ct);
            if (value is null)
            {
                return default;
            }

            try
            {
                // configgure cache expiration
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(1),
                    SlidingExpiration = slidingExpiration
                };

                // Serialize and store the value in Redis
                string serializedValue = JsonSerializer.Serialize(
                    value,
                    JsonOptions);

                await _cache.SetStringAsync(key, serializedValue, options, ct);
            }
            catch (Exception ex)
            {
                // log the error and continue to return the value
                _logger.LogWarning(ex, "Redis SET failed for key {CacheKey}.", key);
            }

            // Return the value retrieved from the factory
            return value;
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default)
        {
            try
            {
                await _cache.RemoveAsync(key, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis REMOVE failed for key {CacheKey}.", key);
            }
        }

    }
}
