using FundedEdge.Domain.Enums;

namespace FundedEdge.Application.Abstractions;

/// <summary>
/// Límites de un plan de suscripción. Valores exactos documentados en
/// GUIA_MONETIZACION_Y_MARKETING.md §3 — esa tabla es la fuente de verdad; si cambian los
/// precios/límites del negocio, se actualizan aquí y allí a la vez.
/// </summary>
/// <param name="AiModelId">Id de modelo Anthropic (wire format, ej. "claude-haiku-4-5") — este proyecto no depende del SDK de Anthropic, el mapeo a su enum vive en FundedEdge.Infrastructure.Ai.</param>
/// <param name="AiEffort">Nombre del miembro del enum Anthropic.Models.Messages.Effort (Low/Medium/High).</param>
/// <param name="CanExportPdf">Export del track record en PDF/impresión (F5.1).</param>
/// <param name="CanPublishPublicProfile">Página pública de track record en /t/{slug} (F5.2).</param>
/// <param name="CanBrowsePeers">Acceso al ranking de perfiles Elite por ROI e informes de inspiración sobre su operativa (F5.6).</param>
public sealed record PlanLimits(
    int? MaxActiveAccounts,
    bool AutoSyncEnabled,
    bool FullRiskModule,
    int AiReportsPerWindow,
    int AiReportWindowDays,
    int? AiQuestionsPerMonth,
    int AiDailyHardCap,
    bool WeeklyAiReportEnabled,
    string AiModelId,
    string AiEffort,
    bool CanExportPdf,
    bool CanPublishPublicProfile,
    bool CanBrowsePeers)
{
    public static PlanLimits For(PlanTier tier) => tier switch
    {
        PlanTier.Starter => new PlanLimits(
            MaxActiveAccounts: 2, AutoSyncEnabled: false, FullRiskModule: false,
            AiReportsPerWindow: 1, AiReportWindowDays: 30, AiQuestionsPerMonth: 3,
            AiDailyHardCap: 50, WeeklyAiReportEnabled: false,
            AiModelId: "claude-haiku-4-5", AiEffort: "Low",
            CanExportPdf: false, CanPublishPublicProfile: false, CanBrowsePeers: false),
        PlanTier.Pro => new PlanLimits(
            MaxActiveAccounts: 10, AutoSyncEnabled: true, FullRiskModule: true,
            AiReportsPerWindow: 1, AiReportWindowDays: 7, AiQuestionsPerMonth: 30,
            AiDailyHardCap: 50, WeeklyAiReportEnabled: true,
            AiModelId: "claude-haiku-4-5", AiEffort: "Medium",
            CanExportPdf: true, CanPublishPublicProfile: false, CanBrowsePeers: false),
        PlanTier.Elite => new PlanLimits(
            MaxActiveAccounts: null, AutoSyncEnabled: true, FullRiskModule: true,
            AiReportsPerWindow: 1, AiReportWindowDays: 1, AiQuestionsPerMonth: null,
            AiDailyHardCap: 50, WeeklyAiReportEnabled: true,
            AiModelId: "claude-opus-4-8", AiEffort: "High",
            CanExportPdf: true, CanPublishPublicProfile: true, CanBrowsePeers: true),
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };
}
