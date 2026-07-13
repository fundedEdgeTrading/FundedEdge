using FundedEdge.Application.Kpis;

namespace FundedEdge.Application.Abstractions;

/// <summary>Estado de la página pública del usuario autenticado, para el toggle en /plan.</summary>
public sealed record PublicProfileSettings(
    string? Slug,
    bool IsEnabled,
    bool CanPublish,
    bool ShareOperativa,
    bool ShareEmotions);

/// <summary>
/// Vista pública de un track record en /t/{slug}. Solo KPIs agregados no monetarios — nunca
/// trades individuales ni importes de costes/payouts (ver GUIA_MONETIZACION_Y_MARKETING.md §F5.2).
/// GUIA_FUNCIONALIDADES_PROPUESTAS.md §3.7: <see cref="IsVerified"/> distingue un track record
/// importado automáticamente de un broker (fiable) de uno introducido a mano (editable a voluntad).
/// </summary>
public sealed record PublicProfileView(
    string DisplayName,
    int AccountsFunded,
    double? PassRate,
    int TotalTrades,
    double? WinRate,
    double? ProfitFactor,
    double? AvgRMultiple,
    bool IsVerified,
    IReadOnlyList<EquityCurvePoint> EquityCurve);
