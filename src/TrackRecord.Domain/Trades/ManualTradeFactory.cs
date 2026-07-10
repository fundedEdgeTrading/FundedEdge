using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Trades;

/// <summary>
/// Construye un round-turn ya agregado (una sola entrada y una sola salida conocidas de
/// antemano) junto con las dos <see cref="Execution"/> que lo representan. Se usa para dos
/// fuentes que entregan datos YA agregados por round-turn, en vez de fills individuales:
/// el journal manual (<see cref="TradeSourceType.Manual"/>) y la importación de los CSV que
/// exportan Tradovate y NinjaTrader 8 (<see cref="TradeSourceType.CsvImport"/>). Un trade
/// manual y uno importado de CSV son indistinguibles a nivel de almacenamiento — solo cambia
/// el TradeSourceType de sus Executions.
///
/// entryExternalId/exitExternalId se piden explícitos (no se derivan de Trade.Id, que es
/// aleatorio) para que el llamador pueda construir identificadores deterministas cuando la
/// idempotencia importa — p.ej. CsvTradeImporter deriva los suyos del contenido de cada fila
/// para que reimportar el mismo CSV no duplique trades.
/// </summary>
public static class ManualTradeFactory
{
    public static Trade Create(
        Guid accountId,
        string symbol,
        TradeDirection direction,
        int quantity,
        decimal avgEntryPrice,
        decimal avgExitPrice,
        DateTimeOffset openedAt,
        DateTimeOffset closedAt,
        decimal grossPnL,
        decimal commissions,
        decimal? riskedAmount,
        string? tags,
        string? notes,
        TradeSourceType source,
        string entryExternalId,
        string exitExternalId,
        decimal? maxAdverseExcursion = null,
        decimal? maxFavorableExcursion = null)
    {
        var normalizedSymbol = symbol.ToUpperInvariant();

        var trade = new Trade
        {
            AccountId = accountId,
            Symbol = normalizedSymbol,
            Direction = direction,
            Quantity = quantity,
            AvgEntryPrice = avgEntryPrice,
            AvgExitPrice = avgExitPrice,
            OpenedAt = openedAt,
            ClosedAt = closedAt,
            GrossPnL = grossPnL,
            Commissions = commissions,
            RiskedAmount = riskedAmount,
            Tags = tags,
            Notes = notes,
            MaxAdverseExcursion = maxAdverseExcursion,
            MaxFavorableExcursion = maxFavorableExcursion,
        };

        var entrySide = direction == TradeDirection.Long ? OrderSide.Buy : OrderSide.Sell;
        var exitSide = direction == TradeDirection.Long ? OrderSide.Sell : OrderSide.Buy;

        // La comisión total se imputa a la pata de salida; es una simplificación deliberada (la
        // fuente no siempre reporta el desglose por fill en datos ya agregados por round-turn),
        // sin impacto en Trade.Commissions, que es el valor autorizado.
        trade.Executions.Add(new Execution
        {
            AccountId = accountId,
            TradeId = trade.Id,
            Trade = trade,
            ExternalId = entryExternalId,
            Source = source,
            Symbol = normalizedSymbol,
            Side = entrySide,
            Quantity = quantity,
            Price = avgEntryPrice,
            ExecutedAt = openedAt,
            Commission = 0m,
        });

        trade.Executions.Add(new Execution
        {
            AccountId = accountId,
            TradeId = trade.Id,
            Trade = trade,
            ExternalId = exitExternalId,
            Source = source,
            Symbol = normalizedSymbol,
            Side = exitSide,
            Quantity = quantity,
            Price = avgExitPrice,
            ExecutedAt = closedAt,
            Commission = commissions,
        });

        return trade;
    }

    /// <summary>Atajo para el caso más común: origen Manual con ids únicos autogenerados.</summary>
    public static Trade CreateManual(
        Guid accountId,
        string symbol,
        TradeDirection direction,
        int quantity,
        decimal avgEntryPrice,
        decimal avgExitPrice,
        DateTimeOffset openedAt,
        DateTimeOffset closedAt,
        decimal grossPnL,
        decimal commissions,
        decimal? riskedAmount,
        string? tags,
        string? notes,
        decimal? maxAdverseExcursion = null,
        decimal? maxFavorableExcursion = null)
    {
        var tradeId = Guid.NewGuid();
        return Create(
            accountId, symbol, direction, quantity, avgEntryPrice, avgExitPrice, openedAt, closedAt,
            grossPnL, commissions, riskedAmount, tags, notes,
            TradeSourceType.Manual, $"manual-{tradeId}-entry", $"manual-{tradeId}-exit",
            maxAdverseExcursion, maxFavorableExcursion);
    }
}
