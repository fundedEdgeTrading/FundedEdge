using Microsoft.EntityFrameworkCore;
using FundedEdge.Application.Abstractions;
using FundedEdge.Application.Dtos;
using FundedEdge.Domain.Enums;
using FundedEdge.Domain.Risk;
using FundedEdge.Infrastructure.Persistence;

namespace FundedEdge.Infrastructure.Services;

/// <summary>
/// Calcula, para cada cuenta activa, cuánto margen queda hoy frente a las reglas de su programa
/// (GUIA_FUNCIONALIDADES_PROPUESTAS.md §2.2/§3.5). Usa las reglas del programa vigente para la
/// etapa actual (fondeada vs evaluación) si la cuenta está enlazada a uno; si no, cae a los
/// campos propios de la cuenta (MaxDrawdown/DrawdownType) y no evalúa pérdida diaria/consistencia.
/// </summary>
public class RuleComplianceService(
    IDbContextFactory<FundedEdgeDbContext> dbFactory,
    ICurrentUserAccessor currentUser) : IRuleComplianceService
{
    private const double YellowThreshold = 0.5;
    private const double RedThreshold = 0.8;

    public async Task<IReadOnlyList<AccountComplianceStatusDto>> GetComplianceStatusAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var accounts = await db.TradingAccounts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Where(a => a.Stage == AccountStage.Evaluation || a.Stage == AccountStage.Funded)
            .Select(a => new
            {
                a.Id,
                a.DisplayName,
                a.Stage,
                a.MaxDrawdown,
                a.DrawdownType,
                ProgramDailyLossLimit = a.EvaluationProgram == null ? (decimal?)null : a.EvaluationProgram.DailyLossLimit,
                ProgramFundedDailyLossLimit = a.EvaluationProgram == null ? (decimal?)null : a.EvaluationProgram.FundedDailyLossLimit,
                ProgramMaxDrawdown = a.EvaluationProgram == null ? (decimal?)null : (decimal?)a.EvaluationProgram.MaxDrawdown,
                ProgramFundedMaxDrawdown = a.EvaluationProgram == null ? (decimal?)null : a.EvaluationProgram.FundedMaxDrawdown,
                ProgramDrawdownType = a.EvaluationProgram == null ? (DrawdownType?)null : (DrawdownType?)a.EvaluationProgram.DrawdownType,
                ProgramFundedDrawdownType = a.EvaluationProgram == null ? (DrawdownType?)null : a.EvaluationProgram.FundedDrawdownType,
                ConsistencyMaxDayFraction = a.EvaluationProgram == null ? (decimal?)null : a.EvaluationProgram.ConsistencyMaxDayFraction,
            })
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var result = new List<AccountComplianceStatusDto>();

        foreach (var account in accounts)
        {
            var isFunded = account.Stage == AccountStage.Funded;
            decimal? dailyLossLimit = isFunded ? account.ProgramFundedDailyLossLimit ?? account.ProgramDailyLossLimit : account.ProgramDailyLossLimit;
            var maxDrawdown = (isFunded ? account.ProgramFundedMaxDrawdown : account.ProgramMaxDrawdown) ?? account.MaxDrawdown;
            var drawdownType = (isFunded ? account.ProgramFundedDrawdownType : account.ProgramDrawdownType) ?? account.DrawdownType;

            var trades = await db.Trades.AsNoTracking()
                .Where(t => t.AccountId == account.Id)
                .OrderBy(t => t.ClosedAt)
                .Select(t => new ComplianceTrade(t.ClosedAt, t.GrossPnL - t.Commissions))
                .ToListAsync(ct);

            var drawdown = ComplianceRuleEngine.EvaluateDrawdown(trades, maxDrawdown, drawdownType);
            var drawdownLevel = LevelFor(drawdown.ConsumedFraction);

            var dailyLoss = ComplianceRuleEngine.EvaluateDailyLoss(trades, dailyLossLimit, today);
            var dailyLossLevel = dailyLoss.ConsumedFraction is double dlFraction ? LevelFor(dlFraction) : ComplianceLevel.Green;

            var consistency = ComplianceRuleEngine.EvaluateConsistency(trades, account.ConsistencyMaxDayFraction);
            var consistencyLevel = consistency is null ? (ComplianceLevel?)null : LevelFor(consistency.ConsumedFraction);

            var overall = new[] { dailyLossLevel, drawdownLevel, consistencyLevel ?? ComplianceLevel.Green }.Max();

            result.Add(new AccountComplianceStatusDto(
                account.Id, account.DisplayName,
                dailyLossLimit, dailyLoss.UsedToday, dailyLoss.Remaining, dailyLossLevel,
                maxDrawdown, drawdown.RemainingBuffer, drawdownLevel,
                account.ConsistencyMaxDayFraction, consistency?.TopDayFraction, consistencyLevel,
                overall));
        }

        return result;
    }

    private static ComplianceLevel LevelFor(double consumedFraction) => consumedFraction switch
    {
        >= RedThreshold => ComplianceLevel.Red,
        >= YellowThreshold => ComplianceLevel.Yellow,
        _ => ComplianceLevel.Green,
    };
}
