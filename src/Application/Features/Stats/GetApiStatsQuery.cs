using MediatR;

namespace Application.Features.Stats;

public sealed record GetApiStatsQuery() : IRequest<IReadOnlyList<ProviderStatsDto>>;
