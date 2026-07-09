namespace TrackRecord.Application.Dtos;

/// <summary>
/// Mapeo de columnas para el import CSV universal (GUIA_FUNCIONALIDADES_PROPUESTAS.md §4.6): el
/// usuario asigna cada campo de Trade a una columna de su CSV (nombre de cabecera exacto), en vez
/// de depender de un formato de broker concreto como hace <see cref="ICsvTradeImportService"/>.
/// Cada valor es el nombre de columna tal y como aparece en la cabecera del CSV subido.
/// </summary>
public record GenericCsvColumnMapping(
    string Symbol,
    string Direction,
    string Quantity,
    string EntryPrice,
    string ExitPrice,
    string OpenedAt,
    string ClosedAt,
    string GrossPnL,
    string? Commissions,
    string? Tags,
    // Máxima pérdida/ganancia flotante durante el trade, si el export del broker las incluye. Opcionales — habilitan MAE/MFE.
    string? MaxAdverseExcursion = null,
    string? MaxFavorableExcursion = null);
