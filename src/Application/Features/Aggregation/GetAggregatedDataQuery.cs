using Application.Models;
using MediatR;

namespace Application.Features.Aggregation;

public sealed record GetAggregatedDataQuery(AggregationRequest Request) : IRequest<AggregationResponse>;
