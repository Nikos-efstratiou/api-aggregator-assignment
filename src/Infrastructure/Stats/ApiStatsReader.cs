using Application.Abstractions;
using Application.Features.Stats;

namespace Infrastructure.Stats;

public sealed class ApiStatsReader : IApiStatsReader
{
    private readonly IApiStatsStore _store;

    public ApiStatsReader(IApiStatsStore store)
    {
        _store = store;
    }

    public IReadOnlyList<ProviderStatsDto> GetStats()
    {
        var snap = _store.Snapshot();

        return snap.Select(kvp =>
        {
            var s = kvp.Value;
            return new ProviderStatsDto(
                Provider: kvp.Key,
                TotalRequests: s.TotalRequests,
                AverageResponseMs: s.AverageResponseMs,
                FastCount: s.FastCount,
                AverageCount: s.AverageCount,
                SlowCount: s.SlowCount
            );
        })
        .OrderBy(x => x.Provider)
        .ToList();
    }
}
