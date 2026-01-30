namespace Application.Features.Stats;

public sealed record ProviderStatsDto(
    string Provider,
    long TotalRequests,
    double AverageResponseMs,
    long FastCount,
    long AverageCount,
    long SlowCount
);
