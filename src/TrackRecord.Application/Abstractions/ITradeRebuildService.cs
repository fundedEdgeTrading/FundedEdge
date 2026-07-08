namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Reconstruye los Trades de una cuenta+símbolo a partir de sus Executions no manuales
/// (Tradovate / NinjaTraderAddOn / CsvImport de fills sueltos) usando TradeBuilder (FIFO).
/// Usado tanto por la ingesta push de NinjaTrader como por el futuro TradeSyncOrchestrator
/// de Tradovate — ver GUIA_IMPLEMENTACION.md §7.
/// </summary>
public interface ITradeRebuildService
{
    /// <summary>
    /// Reconstruye desde cero los Trades de esa cuenta+símbolo: borra los Trades previamente
    /// construidos automáticamente (sin tocar los manuales) y vuelve a agrupar todas las
    /// Executions no manuales disponibles. Devuelve cuántos Trades resultaron.
    /// </summary>
    Task<int> RebuildAsync(Guid accountId, string symbol, CancellationToken ct = default);
}
