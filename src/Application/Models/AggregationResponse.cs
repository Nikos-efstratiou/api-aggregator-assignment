using Domain.Models;

namespace Application.Models;

public sealed class AggregationResponse
{
    public required System.Collections.Generic.IReadOnlyList<AggregatedItem> Items { get; init; }
    public required System.Collections.Generic.IReadOnlyList<AggregationFailure> PartialFailures { get; init; }
}
