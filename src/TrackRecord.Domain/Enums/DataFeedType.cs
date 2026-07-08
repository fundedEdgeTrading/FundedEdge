namespace TrackRecord.Domain.Enums;

/// <summary>
/// Determina qué conector de sincronización usa la cuenta (fase 2). En el MVP
/// solo condiciona el texto informativo mostrado en la ficha de la cuenta.
/// </summary>
public enum DataFeedType
{
    Manual = 0,
    Tradovate = 1,
    NinjaTrader = 2,
}
