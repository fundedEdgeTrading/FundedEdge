using System.Text.Json;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Infrastructure.Services;

/// <summary>
/// Implementación del proveedor de datos externo de programas de evaluación que opera
/// a partir de un JSON estructurado subido manualmente por el usuario desde la UI de Firms.
/// No realiza ninguna petición de red — parsea el string en memoria y valida campos obligatorios.
///
/// Formato esperado del JSON (array de objetos):
/// <code>
/// [
///   {
///     "PropFirmId": "33333333-3333-3333-3333-333333333333",
///     "Name": "Apex 50K",
///     "AccountSize": 50000,
///     "EvaluationCost": 167,
///     "ActivationCost": 130,
///     "ProfitTarget": 3000,
///     "MaxDrawdown": 2500,
///     "DrawdownType": "Trailing",
///     "PayoutSplitTraderPct": 1.0
///   }
/// ]
/// </code>
/// Los campos opcionales (DailyLossLimit, MinTradingDays, etc.) se omiten si no aplican.
/// </summary>
public class ManualExternalFirmDataProvider : IExternalFirmDataProvider
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public Task<IReadOnlyList<EvaluationProgramDto>> FetchProgramsAsync(string sourceJson, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceJson))
            return Task.FromResult<IReadOnlyList<EvaluationProgramDto>>(Array.Empty<EvaluationProgramDto>());

        List<JsonProgramRaw>? raws;
        try
        {
            raws = JsonSerializer.Deserialize<List<JsonProgramRaw>>(sourceJson, _options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"El JSON no tiene un formato válido: {ex.Message}", ex);
        }

        if (raws is null || raws.Count == 0)
            return Task.FromResult<IReadOnlyList<EvaluationProgramDto>>(Array.Empty<EvaluationProgramDto>());

        var result = new List<EvaluationProgramDto>(raws.Count);
        var errors = new List<string>();

        for (int i = 0; i < raws.Count; i++)
        {
            var r = raws[i];
            if (string.IsNullOrWhiteSpace(r.Name))
                errors.Add($"[{i}] El campo 'Name' es obligatorio.");
            if (r.AccountSize <= 0)
                errors.Add($"[{i}] El campo 'AccountSize' debe ser mayor que 0.");
            if (r.ProfitTarget <= 0)
                errors.Add($"[{i}] El campo 'ProfitTarget' debe ser mayor que 0.");
            if (r.MaxDrawdown <= 0)
                errors.Add($"[{i}] El campo 'MaxDrawdown' debe ser mayor que 0.");
            if (r.PayoutSplitTraderPct is <= 0 or > 1)
                errors.Add($"[{i}] El campo 'PayoutSplitTraderPct' debe estar entre 0 (exclusivo) y 1 (inclusivo).");

            if (errors.Count > 0) continue;

            result.Add(new EvaluationProgramDto(
                Id: Guid.NewGuid(),                      // Id provisional hasta que se persista
                PropFirmId: r.PropFirmId,
                PropFirmName: string.Empty,              // Se resuelve en la UI al seleccionar la firma
                Name: r.Name!,
                AccountSize: r.AccountSize,
                EvaluationCost: r.EvaluationCost,
                ActivationCost: r.ActivationCost,
                ProfitTarget: r.ProfitTarget,
                MaxDrawdown: r.MaxDrawdown,
                DrawdownType: r.DrawdownType,
                DailyLossLimit: r.DailyLossLimit,
                MinTradingDays: r.MinTradingDays,
                ConsistencyMaxDayFraction: r.ConsistencyMaxDayFraction,
                FundedMaxDrawdown: r.FundedMaxDrawdown,
                FundedDrawdownType: r.FundedDrawdownType,
                FundedDailyLossLimit: r.FundedDailyLossLimit,
                FundedProfitTarget: r.FundedProfitTarget,
                FundedMinTradingDays: r.FundedMinTradingDays,
                PayoutSplitTraderPct: r.PayoutSplitTraderPct,
                PayoutMaxProfitPct: r.PayoutMaxProfitPct,
                PayoutMinDaysBetween: r.PayoutMinDaysBetween,
                EffectiveFrom: DateOnly.FromDateTime(DateTime.Today),
                IsActive: true));
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"El JSON contiene {errors.Count} error(es) de validación:\n" + string.Join("\n", errors));

        return Task.FromResult<IReadOnlyList<EvaluationProgramDto>>(result);
    }

    /// <summary>Modelo de deserialización interno. Campos opcionales con valores por defecto seguros.</summary>
    private sealed class JsonProgramRaw
    {
        public Guid PropFirmId { get; set; }
        public string? Name { get; set; }
        public decimal AccountSize { get; set; }
        public decimal EvaluationCost { get; set; }
        public decimal ActivationCost { get; set; }
        public decimal ProfitTarget { get; set; }
        public decimal MaxDrawdown { get; set; }
        public DrawdownType DrawdownType { get; set; } = DrawdownType.Trailing;
        public decimal? DailyLossLimit { get; set; }
        public int? MinTradingDays { get; set; }
        public decimal? ConsistencyMaxDayFraction { get; set; }
        public decimal? FundedMaxDrawdown { get; set; }
        public DrawdownType? FundedDrawdownType { get; set; }
        public decimal? FundedDailyLossLimit { get; set; }
        public decimal? FundedProfitTarget { get; set; }
        public int? FundedMinTradingDays { get; set; }
        public decimal PayoutSplitTraderPct { get; set; } = 1.0m;
        public decimal? PayoutMaxProfitPct { get; set; }
        public int? PayoutMinDaysBetween { get; set; }
    }
}
