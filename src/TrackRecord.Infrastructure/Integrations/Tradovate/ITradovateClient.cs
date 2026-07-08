namespace TrackRecord.Infrastructure.Integrations.Tradovate;

public interface ITradovateClient
{
    Task<IReadOnlyList<TradovateAccount>> GetAccountsAsync(string userId, CancellationToken ct = default);

    /// <summary>Fills de la cuenta indicada con timestamp >= since, con el símbolo de contrato ya resuelto.</summary>
    Task<IReadOnlyList<TradovateFill>> GetFillsAsync(string userId, long accountId, DateTimeOffset since, CancellationToken ct = default);
}
