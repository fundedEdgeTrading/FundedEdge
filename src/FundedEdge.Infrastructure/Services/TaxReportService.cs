using Microsoft.EntityFrameworkCore;
using FundedEdge.Application.Abstractions;
using FundedEdge.Application.Dtos;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Persistence;

namespace FundedEdge.Infrastructure.Services;

/// <summary>
/// Agrega payouts cobrados y costes pagados por año natural en criterio de caja
/// (PLAN_IMPLEMENTACION_MERCADO.md M1.2). Solo cuentan los payouts con estado Paid y fecha de
/// cobro informada; los solicitados/aprobados aún no son ingreso.
/// </summary>
public class TaxReportService(
    IDbContextFactory<FundedEdgeDbContext> dbFactory,
    ICurrentUserAccessor currentUser) : ITaxReportService
{
    public async Task<IReadOnlyList<int>> GetAvailableYearsAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var payoutYears = await db.Payouts.AsNoTracking()
            .Where(p => p.Account!.UserId == userId && p.Status == PayoutStatus.Paid && p.PaidOn != null)
            .Select(p => p.PaidOn!.Value.Year)
            .Distinct()
            .ToListAsync(ct);

        var costYears = await db.AccountCosts.AsNoTracking()
            .Where(c => c.Account!.UserId == userId)
            .Select(c => c.PaidOn.Year)
            .Distinct()
            .ToListAsync(ct);

        return payoutYears.Union(costYears).OrderByDescending(y => y).ToList();
    }

    public async Task<TaxYearReportDto> GetYearReportAsync(int year, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var payouts = await db.Payouts.AsNoTracking()
            .Where(p => p.Account!.UserId == userId && p.Status == PayoutStatus.Paid
                && p.PaidOn != null && p.PaidOn.Value.Year == year)
            .OrderBy(p => p.PaidOn)
            .Select(p => new TaxPayoutLineDto(
                p.PaidOn!.Value,
                p.Account!.PropFirm!.Name,
                p.Account.DisplayName,
                p.AmountReceived,
                p.Notes))
            .ToListAsync(ct);

        var costs = await db.AccountCosts.AsNoTracking()
            .Where(c => c.Account!.UserId == userId && c.PaidOn.Year == year)
            .OrderBy(c => c.PaidOn)
            .Select(c => new TaxCostLineDto(
                c.PaidOn,
                c.Account!.PropFirm!.Name,
                c.Account.DisplayName,
                c.Kind,
                c.Amount,
                c.Notes))
            .ToListAsync(ct);

        var quarters = Enumerable.Range(1, 4)
            .Select(q => new TaxQuarterSummaryDto(
                q,
                payouts.Where(p => Quarter(p.PaidOn) == q).Sum(p => p.AmountReceived),
                costs.Where(c => Quarter(c.PaidOn) == q).Sum(c => c.Amount)))
            .ToList();

        return new TaxYearReportDto(year, payouts, costs, quarters);
    }

    private static int Quarter(DateOnly date) => (date.Month - 1) / 3 + 1;
}
