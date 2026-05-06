using Application.Services.Interfaces;
using StackExchange.Redis;
using System.Text.Json;


namespace Persistence.Services
{
    public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
    {
        private readonly IDatabase _database = redis.GetDatabase();

        public async Task<string> GetAsync(string key)
        {
            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return null;

            return value.ToString();
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var serialized = JsonSerializer.Serialize(value);

            if (expiry.HasValue)
            {
                await _database.StringSetAsync(key, serialized, expiry.Value, When.Always);
            }
            else
            {
                await _database.StringSetAsync(key, serialized, null, When.Always);
            }
        }

        public async Task SetRawAsync(string key, string value, TimeSpan? expiry = null)
        {
            if (expiry.HasValue)
            {
                await _database.StringSetAsync(key, value, expiry.Value, When.Always);
            }
            else
            {
                await _database.StringSetAsync(key, value, null, When.Always);
            }
        }

        public async Task RemoveAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }
    }
}
