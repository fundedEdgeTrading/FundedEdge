namespace TrackRecord.Application.Abstractions;

/// <summary>Estado de la página pública del usuario autenticado, para el toggle en /plan.</summary>
public sealed record PublicProfileSettings(string? Slug, bool IsEnabled, bool CanPublish);

/// <summary>
/// Vista pública de un track record en /t/{slug}. Solo KPIs agregados no monetarios — nunca
/// trades individuales ni importes de costes/payouts (ver GUIA_MONETIZACION_Y_MARKETING.md §F5.2).
/// </summary>
public sealed record PublicProfileView(
    string DisplayName,
    int AccountsFunded,
    double? PassRate,
    int TotalTrades,
    double? WinRate,
    double? ProfitFactor,
    double? AvgRMultiple);
