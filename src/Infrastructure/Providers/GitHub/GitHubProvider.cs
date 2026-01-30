using System.Net.Http.Headers;
using System.Text.Json;
using Application.Contracts;
using Application.Models;
using Domain.Models;

namespace Infrastructure.Providers.GitHub;

public sealed class GitHubProvider : IExternalApiProvider
{
    private readonly HttpClient _http;

    public GitHubProvider(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public string Name => "GitHub";

    public async Task<ProviderResult> FetchAsync(AggregationRequest request, CancellationToken ct)
    {
        var q = string.IsNullOrWhiteSpace(request.Q) ? "dotnet" : request.Q.Trim();

        // GitHub search: https://api.github.com/search/repositories?q=dotnet&per_page=10
        var url = $"search/repositories?q={Uri.EscapeDataString(q)}&per_page=10";

        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return ProviderResult.Fail(Name, $"GitHub API returned {(int)resp.StatusCode}");

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);

        var payload = await JsonSerializer.DeserializeAsync<SearchResponse>(stream, JsonOptions, ct);
        var items = payload?.Items ?? new List<RepoItem>();

        var mapped = items.Select(x =>
            new AggregatedItem(
                Source: Name,
                Title: x.FullName ?? x.Name ?? "Unknown",
                Url: x.HtmlUrl,
                Timestamp: x.UpdatedAt,
                Category: "repo"
            )
        ).ToList();

        return ProviderResult.Success(Name, mapped);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed class SearchResponse
    {
        public List<RepoItem>? Items { get; set; }
    }

    private sealed class RepoItem
    {
        public string? Name { get; set; }
        public string? FullName { get; set; }
        public string? HtmlUrl { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
