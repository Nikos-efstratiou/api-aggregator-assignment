using System.Text.Json;
using Application.Contracts;
using Application.Models;
using Domain.Models;

namespace Infrastructure.Providers.Wikipedia;

public sealed class WikipediaProvider : IExternalApiProvider
{
    private readonly HttpClient _http;

    public WikipediaProvider(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public string Name => "Wikipedia";

    public async Task<ProviderResult> FetchAsync(AggregationRequest request, CancellationToken ct)
    {
        var q = string.IsNullOrWhiteSpace(request.Q) ? "dotnet" : request.Q.Trim();

        // Wikipedia Opensearch:
        // https://en.wikipedia.org/w/api.php?action=opensearch&search=dotnet&limit=10&namespace=0&format=json
        var url =
            $"w/api.php?action=opensearch&search={Uri.EscapeDataString(q)}&limit=10&namespace=0&format=json";

        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return ProviderResult.Fail(Name, $"Wikipedia API returned {(int)resp.StatusCode}");

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        // response is array: [searchTerm, titles[], descriptions[], urls[]]
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 4)
            return ProviderResult.Fail(Name, "Unexpected Wikipedia response format");

        var titles = root[1];
        var urls = root[3];

        var results = new List<AggregatedItem>();
        var count = Math.Min(titles.GetArrayLength(), urls.GetArrayLength());

        for (var i = 0; i < count; i++)
        {
            var title = titles[i].GetString() ?? "Unknown";
            var link = urls[i].GetString();

            results.Add(new AggregatedItem(
                Source: Name,
                Title: title,
                Url: link,
                Timestamp: null,     // Wikipedia opensearch doesn't provide timestamps
                Category: "article"
            ));
        }

        return ProviderResult.Success(Name, results);
    }
}
