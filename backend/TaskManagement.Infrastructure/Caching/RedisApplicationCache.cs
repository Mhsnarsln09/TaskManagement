using System.Text.Json;
using StackExchange.Redis;
using TaskManagement.Application.Abstractions;

namespace TaskManagement.Infrastructure.Caching;

public sealed class RedisApplicationCache(IConnectionMultiplexer connection) : IApplicationCache
{
    private readonly IDatabase _database = connection.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        RedisValue value = await _database.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<T>(value.ToString());
    }

    public Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken) where T : class
        => _database.StringSetAsync(key, JsonSerializer.Serialize(value), lifetime);

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
        => _database.KeyDeleteAsync(key);
}
