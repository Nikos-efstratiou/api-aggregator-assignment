using Application.Abstractions;
using Application.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Caching;

public sealed class MemoryAggregationCache : IAggregationCache
{
    private readonly IMemoryCache _cache;

    public MemoryAggregationCache(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public bool TryGet(string key, out AggregationResponse response)
        => _cache.TryGetValue(key, out response!);

    public void Set(string key, AggregationResponse response, TimeSpan ttl)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        _cache.Set(key, response, options);
    }
}
