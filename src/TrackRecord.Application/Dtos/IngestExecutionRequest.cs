using TrackRecord.Domain.Enums;

namespace TrackRecord.Application.Dtos;

/// <summary>
/// Fill crudo procedente de una fuente externa (NinjaTrader 8 vía push, Tradovate vía sync).
/// AccountExternalId debe coincidir con TradingAccount.ExternalAccountId para resolver la cuenta.
/// </summary>
public record IngestExecutionRequest(
    string ExternalId,
    TradeSourceType Source,
    string AccountExternalId,
    string Symbol,
    OrderSide Side,
    int Quantity,
    decimal Price,
    DateTimeOffset ExecutedAt,
    decimal Commission);

public record IngestExecutionResult(bool Inserted, bool AccountResolved, int TradesRebuilt);
