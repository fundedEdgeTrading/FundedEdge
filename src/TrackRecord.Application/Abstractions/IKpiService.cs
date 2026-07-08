using TrackRecord.Application.Kpis;

namespace TrackRecord.Application.Abstractions;

public interface IKpiService
{
    Task<BusinessKpis> GetBusinessKpisAsync(Guid? propFirmId = null, CancellationToken ct = default);
    Task<TradingKpis> GetTradingKpisAsync(Guid? propFirmId = null, Guid? accountId = null, CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyCashflowPoint>> GetMonthlyCashflowAsync(int months = 12, CancellationToken ct = default);
    Task<IReadOnlyList<EquityCurvePoint>> GetEquityCurveAsync(Guid? accountId = null, CancellationToken ct = default);

    /// <summary>Rendimiento por tag/setup entre los trades con Tags informado, ordenado por Net P&amp;L descendente.</summary>
    Task<IReadOnlyList<TagPerformanceDto>> GetTagPerformanceAsync(CancellationToken ct = default);
}
