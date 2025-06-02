using Common_Shared_Services.Interfaces;
using StackExchange.Redis;

namespace Common_Shared_Services.Services
{
    public class CacheService(IConnectionMultiplexer connectionMultiplexer) : ICacheService
    {
        private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

        public async Task<string> GetAsync(string key)
        {
            return await _database.StringGetAsync(key);
        }

        public async Task SetAsync(string key, string value, TimeSpan ttl)
        {
            await _database.StringSetAsync(key, value, ttl);
        }
    }

}
