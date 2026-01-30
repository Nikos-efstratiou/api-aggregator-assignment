namespace Domain.Models;

public sealed record AggregatedItem(
    string Source,
    string Title,
    string? Url,
    System.DateTimeOffset? Timestamp,
    string? Category
);
