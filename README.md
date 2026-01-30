# API Aggregator Assignment

A .NET Web API that aggregates data from multiple external APIs in parallel, supports filtering/sorting, caches results, and exposes per-provider performance stats.

## Tech
- .NET (Web API)
- Clean Architecture style (Api / Application / Domain / Infrastructure)
- MediatR (CQRS query handlers)
- HttpClientFactory + Polly (retry/timeout/circuit breaker)
- In-memory caching (IMemoryCache)
- xUnit + Moq + FluentAssertions

## Run
```bash
dotnet restore
dotnet run --project src/Api


Swagger:

http://localhost:5221/swagger


Endpoints
GET /api/aggregation

Aggregates results from all configured providers.

Query params:

q (string): search term (optional)

category (string): filter by category (optional)

from / to (date-time): filter by timestamp range (optional)

sortBy: date (default), title, source

sortDir: asc or desc (default desc)

Returns:

items: unified list of aggregated results

partialFailures: provider errors (aggregation is resilient; one provider failing does not fail the whole response)

Example:

/api/aggregation?q=dotnet

/api/aggregation?q=London

GET /api/stats

Returns in-memory provider statistics:

total request count

average response time (ms)

bucket counts: fast (<100ms), average (100–200ms), slow (>200ms)

Resilience

Each external provider call uses Polly:

transient retry

timeout

circuit breaker

Caching

Aggregation responses are cached in-memory for a short TTL to reduce external API calls.

Tests
dotnet test


Includes tests for:

merging provider results

partial failures

cache hit behavior

query filtering


---

# Now do these commands (checkpoint)

```powershell
dotnet build
dotnet test
dotnet run --project src\Api


Then check in Swagger:

/api/aggregation?q=dotnet → GitHub URLs/timestamps present + Wikipedia not 403

/api/stats still works