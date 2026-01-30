namespace Application.Models;

public sealed class AggregationRequest
{
    public string? Q { get; init; }
    public string? Category { get; init; }
    public System.DateTimeOffset? From { get; init; }
    public System.DateTimeOffset? To { get; init; }

    public string SortBy { get; init; } = "date";
    public string SortDir { get; init; } = "desc";
}
