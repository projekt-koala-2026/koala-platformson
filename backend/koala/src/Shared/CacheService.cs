using System.Text.Json;
using StackExchange.Redis;

namespace koala.src.Shared
{
    public interface ICacheService
    {
        Task<string?> GetValueAtKeyAsync(string key);
        Task SetValueAtKeyAsync(string key, string value, StackExchange.Redis.Expiration expiry);
        Task RemoveKeyAsync(string key);
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, StackExchange.Redis.Expiration expiry);
    }

    public class CacheService : ICacheService
    {
        private readonly IDatabase _cache;

        public CacheService(IConnectionMultiplexer cache)
        {
            _cache = cache.GetDatabase();
        }

        public async Task<string?> GetValueAtKeyAsync(string key)
        {
            RedisValue value = await _cache.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task SetValueAtKeyAsync(string key, string value, StackExchange.Redis.Expiration expiry)
        {
            await _cache.StringSetAsync(key, value, expiry);
        }

        public async Task RemoveKeyAsync(string key)
        {
            await _cache.KeyDeleteAsync(key);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            RedisValue value = await _cache.StringGetAsync(key);
            if (!value.HasValue)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>((string)value!);
        }

        public async Task SetAsync<T>(string key, T value, StackExchange.Redis.Expiration expiry)
        {
            string json = JsonSerializer.Serialize(value);
            await _cache.StringSetAsync(key, json, expiry);
        }
    }
}