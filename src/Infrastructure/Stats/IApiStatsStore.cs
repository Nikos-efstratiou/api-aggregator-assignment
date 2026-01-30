namespace Infrastructure.Stats;

public interface IApiStatsStore
{
    void Record(string providerName, long elapsedMs);

    IReadOnlyDictionary<string, ProviderStatsSnapshot> Snapshot();
}
