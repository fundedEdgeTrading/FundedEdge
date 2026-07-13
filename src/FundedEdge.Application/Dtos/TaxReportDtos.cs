using FundedEdge.Domain.Enums;

namespace FundedEdge.Application.Dtos;

/// <summary>
/// Informe fiscal anual (PLAN_IMPLEMENTACION_MERCADO.md M1.2): payouts cobrados y costes pagados
/// en el año natural, en criterio de caja — cada línea cuenta por su fecha real de cobro/pago,
/// no por cuándo se solicitó o devengó. Pensado para entregarlo tal cual a una gestoría.
/// </summary>
public sealed record TaxYearReportDto(
    int Year,
    IReadOnlyList<TaxPayoutLineDto> Payouts,
    IReadOnlyList<TaxCostLineDto> Costs,
    IReadOnlyList<TaxQuarterSummaryDto> Quarters)
{
    public decimal TotalPayoutsReceived => Payouts.Sum(p => p.AmountReceived);
    public decimal TotalCosts => Costs.Sum(c => c.Amount);
    public decimal Net => TotalPayoutsReceived - TotalCosts;
}

/// <summary>Payout con estado Paid: ingreso computable del año de su PaidOn.</summary>
public sealed record TaxPayoutLineDto(
    DateOnly PaidOn,
    string FirmName,
    string AccountName,
    decimal AmountReceived,
    string? Notes);

/// <summary>Coste pagado (evaluación, activación, reset, cuotas…): gasto del año de su PaidOn.</summary>
public sealed record TaxCostLineDto(
    DateOnly PaidOn,
    string FirmName,
    string AccountName,
    CostKind Kind,
    decimal Amount,
    string? Notes);

/// <summary>Resumen de un trimestre natural (1–4) del año del informe.</summary>
public sealed record TaxQuarterSummaryDto(int Quarter, decimal PayoutsReceived, decimal Costs)
{
    public decimal Net => PayoutsReceived - Costs;
}
