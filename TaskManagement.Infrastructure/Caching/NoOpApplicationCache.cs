using TaskManagement.Application.Abstractions;

namespace TaskManagement.Infrastructure.Caching;

public sealed class NoOpApplicationCache : IApplicationCache
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
        => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken) where T : class
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
}
