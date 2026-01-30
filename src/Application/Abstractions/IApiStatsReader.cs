using Application.Features.Stats;

namespace Application.Abstractions;

public interface IApiStatsReader
{
    IReadOnlyList<ProviderStatsDto> GetStats();
}
