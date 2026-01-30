using Domain.Models;

namespace Application.Contracts;

public sealed class ProviderResult
{
    public required string Source { get; init; }
    public required bool IsSuccess { get; init; }
    public required System.Collections.Generic.IReadOnlyList<AggregatedItem> Items { get; init; }
    public string? Error { get; init; }

    public static ProviderResult Success(string source, System.Collections.Generic.IReadOnlyList<AggregatedItem> items)
        => new() { Source = source, IsSuccess = true, Items = items };

    public static ProviderResult Fail(string source, string error)
        => new() { Source = source, IsSuccess = false, Items = System.Array.Empty<AggregatedItem>(), Error = error };
}
