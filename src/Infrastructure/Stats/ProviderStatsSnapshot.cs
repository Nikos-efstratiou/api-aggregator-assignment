namespace Infrastructure.Stats;

public sealed record ProviderStatsSnapshot(
    long TotalRequests,
    double AverageResponseMs,
    long FastCount,
    long AverageCount,
    long SlowCount
);
