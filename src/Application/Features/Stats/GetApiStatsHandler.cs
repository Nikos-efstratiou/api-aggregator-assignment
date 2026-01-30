using Application.Abstractions;
using MediatR;

namespace Application.Features.Stats;

public sealed class GetApiStatsHandler : IRequestHandler<GetApiStatsQuery, IReadOnlyList<ProviderStatsDto>>
{
    private readonly IApiStatsReader _statsReader;

    public GetApiStatsHandler(IApiStatsReader statsReader)
    {
        _statsReader = statsReader;
    }

    public Task<IReadOnlyList<ProviderStatsDto>> Handle(GetApiStatsQuery request, CancellationToken ct)
        => Task.FromResult(_statsReader.GetStats());
}
