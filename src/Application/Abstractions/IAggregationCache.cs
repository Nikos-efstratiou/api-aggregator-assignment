using Application.Models;

namespace Application.Abstractions;

public interface IAggregationCache
{
    bool TryGet(string key, out AggregationResponse response);
    void Set(string key, AggregationResponse response, System.TimeSpan ttl);
}
