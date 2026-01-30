using Application.Abstractions;
using Application.Contracts;
using Application.Features.Aggregation;
using Infrastructure.Caching;
using Infrastructure.Providers;
using Infrastructure.Providers.GitHub;
using Infrastructure.Providers.OpenMeteo;
using Infrastructure.Providers.Wikipedia;
using Infrastructure.Stats;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetAggregatedDataQuery).Assembly)
);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAggregationCache, MemoryAggregationCache>();

builder.Services.AddSingleton<IApiStatsStore, InMemoryApiStatsStore>();
builder.Services.AddSingleton<IApiStatsReader, ApiStatsReader>();

static IAsyncPolicy<HttpResponseMessage> GetResiliencePolicy()
{
    var retry = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(200 * attempt));

    var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(2));

    var circuitBreaker = HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(10));

    return Policy.WrapAsync(retry, circuitBreaker, timeout);
}

builder.Services.AddHttpClient<GitHubProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ApiAggregatorAssignment", "1.0"));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
})
.AddPolicyHandler(GetResiliencePolicy());

builder.Services.AddHttpClient<WikipediaProvider>(client =>
{
    client.BaseAddress = new Uri("https://en.wikipedia.org/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ApiAggregatorAssignment", "1.0"));
})
.AddPolicyHandler(GetResiliencePolicy());


builder.Services.AddHttpClient("OpenMeteoGeocoding", client =>
{
    client.BaseAddress = new Uri("https://geocoding-api.open-meteo.com/");
})
.AddPolicyHandler(GetResiliencePolicy());

builder.Services.AddHttpClient("OpenMeteoForecast", client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
})
.AddPolicyHandler(GetResiliencePolicy());

builder.Services.AddSingleton<OpenMeteoProvider>();

builder.Services.AddSingleton<IExternalApiProvider>(sp =>
    new StatsDecoratedProvider(sp.GetRequiredService<GitHubProvider>(), sp.GetRequiredService<IApiStatsStore>())
);

builder.Services.AddSingleton<IExternalApiProvider>(sp =>
    new StatsDecoratedProvider(sp.GetRequiredService<WikipediaProvider>(), sp.GetRequiredService<IApiStatsStore>())
);

builder.Services.AddSingleton<IExternalApiProvider>(sp =>
    new StatsDecoratedProvider(sp.GetRequiredService<OpenMeteoProvider>(), sp.GetRequiredService<IApiStatsStore>())
);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
