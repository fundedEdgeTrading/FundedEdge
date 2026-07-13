using FundedEdge.Application.Dtos;

namespace FundedEdge.Application.Abstractions;

/// <summary>
/// Informe fiscal anual de payouts y costes (PLAN_IMPLEMENTACION_MERCADO.md M1.2): agrega en
/// criterio de caja los payouts cobrados y los costes pagados del usuario actual, por año natural
/// y trimestre, listos para exportar a CSV para la gestoría.
/// </summary>
public interface ITaxReportService
{
    /// <summary>Años naturales con al menos un payout cobrado o un coste pagado, descendente.</summary>
    Task<IReadOnlyList<int>> GetAvailableYearsAsync(CancellationToken ct = default);

    /// <summary>Informe del año indicado. Devuelve un informe vacío si no hay movimientos.</summary>
    Task<TaxYearReportDto> GetYearReportAsync(int year, CancellationToken ct = default);
}
