namespace TrackRecord.Domain.Enums;

/// <summary>
/// Origen de una <see cref="Entities.Execution"/>. Las fuentes activas son Manual y CsvImport
/// (importación del CSV de Tradovate/NinjaTrader 8); Tradovate y NinjaTraderAddOn se conservan
/// solo como valores legados para datos ingeridos por las integraciones por API ya retiradas.
/// </summary>
public enum TradeSourceType
{
    Manual = 0,
    Tradovate = 1,
    NinjaTraderAddOn = 2,
    CsvImport = 3,
}
