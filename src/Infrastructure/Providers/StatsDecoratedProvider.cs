using System.Diagnostics;
using Application.Contracts;
using Application.Models;
using Infrastructure.Stats;

namespace Infrastructure.Providers;

public sealed class StatsDecoratedProvider : IExternalApiProvider
{
    private readonly IExternalApiProvider _inner;
    private readonly IApiStatsStore _stats;

    public StatsDecoratedProvider(IExternalApiProvider inner, IApiStatsStore stats)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _stats = stats ?? throw new ArgumentNullException(nameof(stats));
    }

    public string Name => _inner.Name;

    public async Task<ProviderResult> FetchAsync(AggregationRequest request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await _inner.FetchAsync(request, ct);
        }
        finally
        {
            sw.Stop();
            _stats.Record(Name, sw.ElapsedMilliseconds);
        }
    }
}
