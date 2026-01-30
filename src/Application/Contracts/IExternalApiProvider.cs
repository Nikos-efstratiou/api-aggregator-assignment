using Application.Models;

namespace Application.Contracts;

public interface IExternalApiProvider
{
    string Name { get; }
    System.Threading.Tasks.Task<ProviderResult> FetchAsync(AggregationRequest request, System.Threading.CancellationToken ct);
}
