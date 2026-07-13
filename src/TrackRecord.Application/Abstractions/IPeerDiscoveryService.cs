using TrackRecord.Application.Kpis;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Descubrimiento de perfiles Elite (F5.6): ranking de los traders con mejor ROI de negocio cuya
/// página pública está activa, para que otro usuario Elite se inspire en su operativa. Todo el
/// acceso está restringido al plan Elite del que consulta y, para el detalle de operativa, al
/// opt-in explícito del dueño (PublicProfile.ShareOperativa/ShareEmotions). Solo agregados —
/// nunca trades individuales ni importes monetarios absolutos.
/// </summary>
public interface IPeerDiscoveryService
{
    /// <summary>
    /// Ranking de perfiles Elite por ROI de negocio, descendente. Requiere que el usuario actual
    /// sea Elite; si no lo es devuelve lista vacía. Excluye el propio perfil del usuario.
    /// </summary>
    Task<IReadOnlyList<PeerCardView>> GetLeaderboardAsync(int take = 20, CancellationToken ct = default);

    /// <summary>
    /// Vista de análisis enriquecida de un par por slug (setups, franjas horarias, R-múltiplos y
    /// —si el dueño lo permitió— patrón emocional agregado). Null si el usuario actual no es Elite,
    /// el perfil no existe/está deshabilitado, el dueño ya no es Elite o no dio opt-in de operativa.
    /// </summary>
    Task<PeerAnalysisView?> GetPeerAnalysisAsync(string slug, CancellationToken ct = default);
}

/// <summary>Tarjeta de un perfil en el ranking Elite. Solo métricas agregadas no monetarias.</summary>
/// <param name="SharesOperativa">Si el dueño dio opt-in para que se analice su operativa (habilita el informe de inspiración).</param>
public sealed record PeerCardView(
    string Slug,
    string DisplayName,
    double BusinessRoi,
    int AccountsFunded,
    double? PassRate,
    double? ProfitFactor,
    double? WinRate,
    int TotalTrades,
    bool IsVerified,
    bool SharesOperativa);

/// <summary>
/// Datos agregados de la operativa de un par para que el sistema genere el informe de inspiración.
/// <see cref="Emotions"/> solo viene poblado si el dueño activó <c>ShareEmotions</c>.
/// </summary>
public sealed record PeerAnalysisView(
    string UserId,
    string Slug,
    string DisplayName,
    bool IsVerified,
    double BusinessRoi,
    int AccountsFunded,
    double? PassRate,
    int TotalTrades,
    double? WinRate,
    double? ProfitFactor,
    double? AvgRMultiple,
    IReadOnlyList<TagPerformanceDto> TopSetups,
    IReadOnlyList<EquityCurvePoint> EquityCurve,
    PeerEmotionSummary? Emotions);

/// <summary>Resumen emocional agregado de un par (opt-in). Nunca notas ni logs crudos.</summary>
public sealed record PeerEmotionSummary(
    IReadOnlyList<PeerEmotionFrequency> MostFrequentEntryEmotions,
    double FollowedPlanPct);

public sealed record PeerEmotionFrequency(string Emotion, int Count);
