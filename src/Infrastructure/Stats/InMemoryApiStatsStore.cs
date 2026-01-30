using System.Collections.Concurrent;

namespace Infrastructure.Stats;

public sealed class InMemoryApiStatsStore : IApiStatsStore
{
    private sealed class ProviderStats
    {
        public long TotalRequests;
        public long TotalElapsedMs;

        public long FastCount;    // <100ms
        public long AverageCount; // 100-200ms
        public long SlowCount;    // >200ms
    }

    private readonly ConcurrentDictionary<string, ProviderStats> _stats = new();

    public void Record(string providerName, long elapsedMs)
    {
        var s = _stats.GetOrAdd(providerName, _ => new ProviderStats());

        Interlocked.Increment(ref s.TotalRequests);
        Interlocked.Add(ref s.TotalElapsedMs, elapsedMs);

        if (elapsedMs < 100)
            Interlocked.Increment(ref s.FastCount);
        else if (elapsedMs <= 200)
            Interlocked.Increment(ref s.AverageCount);
        else
            Interlocked.Increment(ref s.SlowCount);
    }

    public IReadOnlyDictionary<string, ProviderStatsSnapshot> Snapshot()
    {
        return _stats.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var s = kvp.Value;
                var total = Volatile.Read(ref s.TotalRequests);
                var elapsed = Volatile.Read(ref s.TotalElapsedMs);

                var avg = total == 0 ? 0 : (double)elapsed / total;

                return new ProviderStatsSnapshot(
                    TotalRequests: total,
                    AverageResponseMs: avg,
                    FastCount: Volatile.Read(ref s.FastCount),
                    AverageCount: Volatile.Read(ref s.AverageCount),
                    SlowCount: Volatile.Read(ref s.SlowCount)
                );
            }
        );
    }
}
