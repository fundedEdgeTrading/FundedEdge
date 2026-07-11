using System.Text.Json;
using System.Text.Json.Serialization;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Application.Dtos;

/// <summary>
/// Reglas de un programa extraídas por el LLM de una página oficial. Campos null = el dato no
/// aparece en la página (nunca se inventa); al aprobar sobre un programa existente, los null
/// conservan el valor actual. Importes en USD absolutos y porcentajes como fracción 0-1,
/// el mismo formato que <see cref="UpsertEvaluationProgramRequest"/>.
/// </summary>
public record ExtractedProgramRules(
    string Name,
    decimal AccountSize,
    decimal? EvaluationCost,
    decimal? ActivationCost,
    decimal? ProfitTarget,
    decimal? MaxDrawdown,
    DrawdownType? DrawdownType,
    decimal? DailyLossLimit,
    int? MinTradingDays,
    decimal? ConsistencyMaxDayFraction,
    decimal? FundedMaxDrawdown,
    DrawdownType? FundedDrawdownType,
    decimal? FundedDailyLossLimit,
    decimal? FundedProfitTarget,
    int? FundedMinTradingDays,
    decimal? PayoutSplitTraderPct,
    decimal? PayoutMaxProfitPct,
    int? PayoutMinDaysBetween,
    /// <summary>Confianza global 0-1 declarada por el modelo para este programa.</summary>
    decimal Confidence,
    /// <summary>Citas literales de la página que sustentan cada campo extraído (verificación anti-alucinación).</summary>
    IReadOnlyList<FieldQuote> Quotes);

/// <summary>Cita literal de la página fuente que sustenta el valor de un campo extraído.</summary>
public record FieldQuote(string Field, string Quote);

/// <summary>Diferencia de un campo entre el programa activo y la propuesta extraída.</summary>
public record ProgramFieldDiff(string Field, string? CurrentValue, string ProposedValue, string? Quote);

/// <summary>Propuesta pendiente con su diff calculado contra el catálogo activo, lista para revisar.</summary>
public record ProposedProgramChangeDto(
    Guid Id,
    Guid PropFirmId,
    string PropFirmName,
    string ProgramName,
    Guid? ExistingProgramId,
    string? SourceUrl,
    ProposalStatus Status,
    DateTimeOffset CreatedAt,
    decimal Confidence,
    IReadOnlyList<ProgramFieldDiff> Diffs);

/// <summary>Opciones de (de)serialización del payload de extracción (enums por nombre, camelCase).</summary>
public static class RuleExtractionJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
