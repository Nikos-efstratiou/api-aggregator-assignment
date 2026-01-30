using System.Globalization;
using System.Text.Json;
using Application.Contracts;
using Application.Models;
using Domain.Models;

namespace Infrastructure.Providers.OpenMeteo;

public sealed class OpenMeteoProvider : IExternalApiProvider
{
    private readonly IHttpClientFactory _httpFactory;

    public OpenMeteoProvider(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
    }

    public string Name => "OpenMeteo";

    public async Task<ProviderResult> FetchAsync(AggregationRequest request, CancellationToken ct)
    {
        var city = string.IsNullOrWhiteSpace(request.Q) ? "Athens" : request.Q.Trim();

        // Named clients
        var geoClient = _httpFactory.CreateClient("OpenMeteoGeocoding");
        var forecastClient = _httpFactory.CreateClient("OpenMeteoForecast");

        // 1) Geocoding API
        var geoUrl = $"v1/search?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json";

        using var geoResp = await geoClient.GetAsync(geoUrl, ct);
        if (!geoResp.IsSuccessStatusCode)
            return ProviderResult.Fail(Name, $"Geocoding returned {(int)geoResp.StatusCode}");

        await using var geoStream = await geoResp.Content.ReadAsStreamAsync(ct);
        using var geoDoc = await JsonDocument.ParseAsync(geoStream, cancellationToken: ct);

        if (!geoDoc.RootElement.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array ||
            results.GetArrayLength() == 0)
        {
            return ProviderResult.Success(Name, Array.Empty<AggregatedItem>());
        }

        var first = results[0];
 static double ReadLatitude(JsonElement el)
{
    var v = el.GetProperty("latitude").GetDouble();
    while (Math.Abs(v) > 90) v /= 1000;
    return v;
}

static double ReadLongitude(JsonElement el)
{
    var v = el.GetProperty("longitude").GetDouble();
    while (Math.Abs(v) > 180) v /= 1000;
    return v;
}


var lat = ReadLatitude(first);
var lon = ReadLongitude(first);

if (lat is < -90 or > 90 || lon is < -180 or > 180)
    return ProviderResult.Fail(Name, $"Invalid coordinates after normalization: lat={lat}, lon={lon}");

Console.WriteLine($"[OpenMeteo] city={city} lat={lat} lon={lon}");


        var resolvedName = first.GetProperty("name").GetString() ?? city;

      // 2) Forecast API (current conditions)
var latStr = lat.ToString(CultureInfo.InvariantCulture);
var lonStr = lon.ToString(CultureInfo.InvariantCulture);

var forecastUrl =
    $"v1/forecast?latitude={latStr}&longitude={lonStr}&current=temperature_2m,wind_speed_10m&timezone=auto";


using var foreResp = await forecastClient.GetAsync(forecastUrl, ct);
if (!foreResp.IsSuccessStatusCode)
{
    var body = await foreResp.Content.ReadAsStringAsync(ct);
    return ProviderResult.Fail(Name, $"Forecast returned {(int)foreResp.StatusCode}: {body}");
}

await using var foreStream = await foreResp.Content.ReadAsStreamAsync(ct);
using var foreDoc = await JsonDocument.ParseAsync(foreStream, cancellationToken: ct);

if (!foreDoc.RootElement.TryGetProperty("current", out var cur))
    return ProviderResult.Fail(Name, "Missing current");

var temp = cur.TryGetProperty("temperature_2m", out var t) ? t.GetDouble() : (double?)null;
var wind = cur.TryGetProperty("wind_speed_10m", out var w) ? w.GetDouble() : (double?)null;
var timeStr = cur.TryGetProperty("time", out var tm) ? tm.GetString() : null;

DateTimeOffset? timestamp = null;
if (DateTimeOffset.TryParse(timeStr, out var parsed))
    timestamp = parsed;

var item = new AggregatedItem(
    Source: Name,
    Title: $"Weather in {resolvedName}: {temp?.ToString() ?? "?"}°C, wind {wind?.ToString() ?? "?"} km/h",
    Url: null,
    Timestamp: timestamp,
    Category: "weather"
);

return ProviderResult.Success(Name, new[] { item });

    }
}
