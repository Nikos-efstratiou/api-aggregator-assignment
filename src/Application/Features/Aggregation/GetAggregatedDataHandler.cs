using Application.Abstractions;
using Application.Contracts;
using Application.Models;
using Domain.Models;
using MediatR;

namespace Application.Features.Aggregation;

public sealed class GetAggregatedDataHandler : IRequestHandler<GetAggregatedDataQuery, AggregationResponse>
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromSeconds(60);

    private readonly IEnumerable<IExternalApiProvider> _providers;
    private readonly IAggregationCache _cache;

    public GetAggregatedDataHandler(IEnumerable<IExternalApiProvider> providers, IAggregationCache cache)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<AggregationResponse> Handle(GetAggregatedDataQuery query, CancellationToken ct)
    {
        var request = query.Request ?? new AggregationRequest();
        var cacheKey = BuildCacheKey(request);

        if (_cache.TryGet(cacheKey, out var cached))
            return cached;

        var tasks = _providers.Select(p => SafeFetchAsync(p, request, ct)).ToArray();
        var results = await Task.WhenAll(tasks);

        var failures = results
            .Where(r => !r.IsSuccess)
            .Select(r => new AggregationFailure(r.Source, r.Error ?? "Unknown error"))
            .ToList();

        var items = results
            .Where(r => r.IsSuccess)
            .SelectMany(r => r.Items)
            .ToList();

        var filteredSorted = ApplyFilteringAndSorting(items, request);

        var response = new AggregationResponse
        {
            Items = filteredSorted,
            PartialFailures = failures
        };

        _cache.Set(cacheKey, response, DefaultCacheTtl);

        return response;
    }

    private static async Task<ProviderResult> SafeFetchAsync(IExternalApiProvider provider, AggregationRequest request, CancellationToken ct)
    {
        try
        {
            return await provider.FetchAsync(request, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ProviderResult.Fail(provider.Name, $"Unhandled provider exception: {ex.Message}");
        }
    }

    private static IReadOnlyList<AggregatedItem> ApplyFilteringAndSorting(List<AggregatedItem> items, AggregationRequest request)
    {
        IEnumerable<AggregatedItem> q = items;

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var term = request.Q.Trim();
            q = q.Where(x => x.Title.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var cat = request.Category.Trim();
            q = q.Where(x => string.Equals(x.Category, cat, StringComparison.OrdinalIgnoreCase));
        }

        if (request.From is not null)
            q = q.Where(x => x.Timestamp is null || x.Timestamp >= request.From);

        if (request.To is not null)
            q = q.Where(x => x.Timestamp is null || x.Timestamp <= request.To);

        var sortBy = (request.SortBy ?? "date").Trim().ToLowerInvariant();
        var sortDir = (request.SortDir ?? "desc").Trim().ToLowerInvariant();

        Func<AggregatedItem, object?> keySelector = sortBy switch
        {
            "title" => x => x.Title,
            "source" => x => x.Source,
            _ => x => x.Timestamp ?? DateTimeOffset.MinValue
        };

        q = (sortDir == "asc") ? q.OrderBy(keySelector) : q.OrderByDescending(keySelector);

        return q.Take(200).ToList();
    }

    private static string BuildCacheKey(AggregationRequest r)
    {
        return string.Join("|",
            "agg",
            (r.Q ?? "").Trim().ToLowerInvariant(),
            (r.Category ?? "").Trim().ToLowerInvariant(),
            r.From?.ToUnixTimeSeconds().ToString() ?? "",
            r.To?.ToUnixTimeSeconds().ToString() ?? "",
            (r.SortBy ?? "date").Trim().ToLowerInvariant(),
            (r.SortDir ?? "desc").Trim().ToLowerInvariant()
        );
    }
}
