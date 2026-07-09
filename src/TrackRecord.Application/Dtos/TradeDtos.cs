using TrackRecord.Domain.Enums;

namespace TrackRecord.Application.Dtos;

public record TradeListItemDto(
    Guid Id,
    Guid AccountId,
    string AccountDisplayName,
    string Symbol,
    TradeDirection Direction,
    int Quantity,
    decimal AvgEntryPrice,
    decimal AvgExitPrice,
    DateTimeOffset OpenedAt,
    DateTimeOffset ClosedAt,
    decimal NetPnL,
    decimal? RMultiple,
    string? Tags,
    decimal? MaeR = null,
    decimal? MfeR = null);

public record CreateTradeRequest(
    Guid AccountId,
    string Symbol,
    TradeDirection Direction,
    int Quantity,
    decimal AvgEntryPrice,
    decimal AvgExitPrice,
    DateTimeOffset OpenedAt,
    DateTimeOffset ClosedAt,
    decimal GrossPnL,
    decimal Commissions,
    decimal? RiskedAmount,
    string? Tags,
    string? Notes,
    // Máxima pérdida/ganancia flotante (en $) durante el trade. Opcionales — habilitan MAE/MFE.
    decimal? MaxAdverseExcursion = null,
    decimal? MaxFavorableExcursion = null);
