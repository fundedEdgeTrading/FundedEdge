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

    /// <summary>P&amp;L del negocio pivotado por firma — ROI, coste por cuenta fondeada, tiempo a payout, tasa de quema.</summary>
    Task<IReadOnlyList<FirmBusinessBreakdownDto>> GetFirmBusinessBreakdownAsync(CancellationToken ct = default);

    /// <summary>Expectancy por día de la semana y hora de entrada.</summary>
    Task<IReadOnlyList<TimeOfDayPerformancePoint>> GetTimeOfDayHeatmapAsync(CancellationToken ct = default);

    /// <summary>Duración media de trades ganadores vs perdedores.</summary>
    Task<DurationAsymmetryDto> GetDurationAsymmetryAsync(CancellationToken ct = default);

    /// <summary>Calidad de ejecución (MAE/MFE) sobre los trades con excursión registrada por el usuario.</summary>
    Task<ExecutionQualityDto> GetExecutionQualityAsync(CancellationToken ct = default);
}
