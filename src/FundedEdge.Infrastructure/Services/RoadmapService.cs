using FundedEdge.Application.Abstractions;
using FundedEdge.Application.Dtos;
using FundedEdge.Domain.Enums;

namespace FundedEdge.Infrastructure.Services;

public class RoadmapService(ITradingAccountService accountService) : IRoadmapService
{
    public async Task<RoadmapDto> GetRoadmapAsync(
        decimal reinvestPercent = 0.4m,
        decimal personalPercent = 0.4m,
        decimal taxPercent = 0.2m,
        CancellationToken ct = default)
    {
        var accounts = await accountService.GetAllAsync(ct: ct);

        var fundedPriority = accounts
            .Where(a => a.Stage == AccountStage.Funded)
            .Select(a => new RoadmapAccountItemDto(
                a.Id,
                a.DisplayName,
                a.PropFirmName,
                a.Stage,
                AvailableToWithdraw: Math.Max(0, a.NetPnL - a.TotalPayoutsReceived),
                ProgressPercent: a.EffectiveProfitTarget > 0 ? Math.Min(100, a.NetPnL / a.EffectiveProfitTarget * 100) : 0))
            .OrderByDescending(a => a.AvailableToWithdraw)
            .ToList();

        var evaluationPriority = accounts
            .Where(a => a.Stage == AccountStage.Evaluation)
            .Select(a => new RoadmapAccountItemDto(
                a.Id,
                a.DisplayName,
                a.PropFirmName,
                a.Stage,
                AvailableToWithdraw: 0,
                ProgressPercent: a.EffectiveProfitTarget > 0 ? Math.Min(100, a.NetPnL / a.EffectiveProfitTarget * 100) : 0))
            .OrderByDescending(a => a.ProgressPercent)
            .ToList();

        var totalPayouts = accounts.Sum(a => a.TotalPayoutsReceived);
        var reinvestment = new ReinvestmentPlanDto(
            totalPayouts,
            reinvestPercent,
            personalPercent,
            taxPercent,
            totalPayouts * reinvestPercent,
            totalPayouts * personalPercent,
            totalPayouts * taxPercent);

        return new RoadmapDto(fundedPriority, evaluationPriority, reinvestment);
    }
}
