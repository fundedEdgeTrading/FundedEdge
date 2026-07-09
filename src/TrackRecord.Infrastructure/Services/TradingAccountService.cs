using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Domain.Trades;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

public class TradingAccountService(
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    ICurrentUserAccessor currentUser,
    IPlanService planService) : ITradingAccountService
{
    public async Task<IReadOnlyList<TradingAccountListItemDto>> GetAllAsync(
        AccountStage? stageFilter = null, Guid? propFirmId = null, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var query = db.TradingAccounts.AsNoTracking().Include(a => a.PropFirm)
            .Where(a => a.UserId == userId)
            .AsQueryable();
        if (stageFilter is not null) query = query.Where(a => a.Stage == stageFilter);
        if (propFirmId is not null) query = query.Where(a => a.PropFirmId == propFirmId);

        return await query
            .OrderByDescending(a => a.PurchasedOn)
            .Select(a => new TradingAccountListItemDto(
                a.Id,
                a.DisplayName,
                a.PropFirmId,
                a.PropFirm!.Name,
                a.AccountSize,
                a.Stage,
                a.Feed,
                a.ExternalAccountId,
                a.PurchasedOn,
                a.FundedOn,
                a.ClosedOn,
                a.Trades.Sum(t => (decimal?)(t.GrossPnL - t.Commissions)) ?? 0m,
                a.Costs.Sum(c => (decimal?)c.Amount) ?? 0m,
                a.Payouts.Sum(p => (decimal?)p.AmountReceived) ?? 0m))
            .ToListAsync(ct);
    }

    public async Task<TradingAccountDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var account = await db.TradingAccounts
            .AsNoTracking()
            .Include(a => a.PropFirm)
            .Include(a => a.Events)
            .Include(a => a.Costs)
            .Include(a => a.Payouts)
            .Include(a => a.Trades)
            .SingleOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);

        if (account is null) return null;

        // Cuenta atrás al próximo payout elegible: solo si la firma tiene la regla configurada y
        // la cuenta está fondeada. Punto de partida: el último payout solicitado, o la fecha de
        // fondeo si todavía no ha pedido ninguno.
        DateOnly? nextPayoutEligibleOn = null;
        if (account.Stage == AccountStage.Funded && account.PropFirm!.MinDaysBetweenPayouts is { } minDays)
        {
            var lastPayoutOn = account.Payouts.Count > 0 ? account.Payouts.Max(p => p.RequestedOn) : (DateOnly?)null;
            var baseDate = lastPayoutOn ?? account.FundedOn;
            nextPayoutEligibleOn = baseDate?.AddDays(minDays);
        }

        return new TradingAccountDetailDto(
            account.Id,
            account.PropFirmId,
            account.PropFirm!.Name,
            account.DisplayName,
            account.ExternalAccountId,
            account.AccountSize,
            account.ProfitTarget,
            account.MaxDrawdown,
            account.DrawdownType,
            account.Stage,
            account.Feed,
            account.PurchasedOn,
            account.FundedOn,
            account.ClosedOn,
            account.Notes,
            account.Events.OrderBy(e => e.OccurredAt)
                .Select(e => new AccountEventDto(e.Id, e.FromStage, e.ToStage, e.OccurredAt, e.Notes))
                .ToList(),
            account.Costs.OrderBy(c => c.PaidOn)
                .Select(c => new AccountCostDto(c.Id, c.Kind, c.Amount, c.PaidOn, c.Notes))
                .ToList(),
            account.Payouts.OrderBy(p => p.RequestedOn)
                .Select(p => new PayoutDto(p.Id, p.AmountRequested, p.AmountReceived, p.RequestedOn, p.PaidOn, p.Status, p.Notes))
                .ToList(),
            account.Trades.OrderByDescending(t => t.ClosedAt)
                .Select(t => new TradeListItemDto(
                    t.Id, t.AccountId, account.DisplayName, t.Symbol, t.Direction, t.Quantity,
                    t.AvgEntryPrice, t.AvgExitPrice, t.OpenedAt, t.ClosedAt,
                    t.GrossPnL - t.Commissions, t.RiskedAmount is > 0 ? (t.GrossPnL - t.Commissions) / t.RiskedAmount.Value : null,
                    t.Tags,
                    t.RiskedAmount is > 0 && t.MaxAdverseExcursion != null ? t.MaxAdverseExcursion / t.RiskedAmount.Value : null,
                    t.RiskedAmount is > 0 && t.MaxFavorableExcursion != null ? t.MaxFavorableExcursion / t.RiskedAmount.Value : null))
                .ToList(),
            nextPayoutEligibleOn,
            account.EvaluationProgramId);
    }

    public async Task<Guid> CreateAsync(CreateTradingAccountRequest request, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();

        if (!await planService.CanCreateAccountAsync(ct))
        {
            throw new InvalidOperationException(
                "Tu plan actual no permite más cuentas activas. Mejora a Pro o Elite en /plan.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Si se seleccionó un programa del catálogo, autocompletar los campos de reglas.
        decimal accountSize    = request.AccountSize;
        decimal profitTarget   = request.ProfitTarget;
        decimal maxDrawdown    = request.MaxDrawdown;
        DrawdownType drawdown  = request.DrawdownType;
        decimal? evaluationCost = request.EvaluationCost;

        if (request.EvaluationProgramId is { } programId)
        {
            var program = await db.EvaluationPrograms
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == programId, ct);
            if (program is not null)
            {
                accountSize    = program.AccountSize;
                profitTarget   = program.ProfitTarget;
                maxDrawdown    = program.MaxDrawdown;
                drawdown       = program.DrawdownType;
                evaluationCost ??= program.EvaluationCost;
            }
        }

        var account = new TradingAccount
        {
            UserId = userId,
            PropFirmId = request.PropFirmId,
            EvaluationProgramId = request.EvaluationProgramId,
            DisplayName = request.DisplayName,
            ExternalAccountId = request.ExternalAccountId,
            AccountSize = accountSize,
            ProfitTarget = profitTarget,
            MaxDrawdown = maxDrawdown,
            DrawdownType = drawdown,
            Feed = request.Feed,
            PurchasedOn = request.PurchasedOn,
            Notes = request.Notes,
            Stage = AccountStage.Evaluation,
        };

        account.Events.Add(new AccountEvent
        {
            AccountId = account.Id,
            FromStage = AccountStage.Evaluation,
            ToStage = AccountStage.Evaluation,
            OccurredAt = request.PurchasedOn.ToDateTime(TimeOnly.MinValue),
            Notes = "Cuenta comprada.",
        });

        if (evaluationCost is > 0)
        {
            account.Costs.Add(new AccountCost
            {
                AccountId = account.Id,
                Kind = CostKind.Evaluation,
                Amount = evaluationCost.Value,
                PaidOn = request.PurchasedOn,
            });
        }

        db.TradingAccounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account.Id;
    }

    public async Task TransitionStageAsync(TransitionAccountStageRequest request, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var account = await db.TradingAccounts.SingleOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Cuenta {request.AccountId} no encontrada.");

        await db.Entry(account).Collection(a => a.Events).LoadAsync(ct);

        var newEvent = account.TransitionTo(request.NewStage, request.OccurredAt, request.Notes);

        db.AccountEvents.Add(newEvent);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateConnectionAsync(UpdateAccountConnectionRequest request, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var account = await db.TradingAccounts.SingleOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Cuenta {request.AccountId} no encontrada.");

        account.Feed = request.Feed;
        account.ExternalAccountId = string.IsNullOrWhiteSpace(request.ExternalAccountId) ? null : request.ExternalAccountId;
        await db.SaveChangesAsync(ct);
    }

    public async Task RenameAsync(RenameAccountRequest request, CancellationToken ct = default)
    {
        var displayName = request.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("El nombre de la cuenta no puede estar vacío.", nameof(request));
        }
        if (displayName.Length > 200)
        {
            throw new ArgumentException("El nombre de la cuenta no puede superar los 200 caracteres.", nameof(request));
        }

        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var account = await db.TradingAccounts.SingleOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Cuenta {request.AccountId} no encontrada.");

        account.DisplayName = displayName;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Elimina la cuenta y todo lo que cuelga de ella. Las FK a TradingAccounts son
    /// DeleteBehavior.Restrict (evita ciclos de cascada en SQL Server — ver
    /// TradingAccountConfiguration/ExecutionConfiguration), así que el borrado en cascada no lo
    /// hace la base de datos: se cargan y marcan para borrar aquí, y SaveChangesAsync ordena las
    /// sentencias DELETE según el grafo de FK (hijos antes que la cuenta) en una única transacción.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var account = await db.TradingAccounts.SingleOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (account is null) return;

        var executions = await db.Executions.Where(e => e.AccountId == id).ToListAsync(ct);
        var trades = await db.Trades.Where(t => t.AccountId == id).ToListAsync(ct);
        var payouts = await db.Payouts.Where(p => p.AccountId == id).ToListAsync(ct);
        var costs = await db.AccountCosts.Where(c => c.AccountId == id).ToListAsync(ct);
        var events = await db.AccountEvents.Where(e => e.AccountId == id).ToListAsync(ct);

        db.Executions.RemoveRange(executions);
        db.Trades.RemoveRange(trades);
        db.Payouts.RemoveRange(payouts);
        db.AccountCosts.RemoveRange(costs);
        db.AccountEvents.RemoveRange(events);
        db.TradingAccounts.Remove(account);

        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> AddCostAsync(AddAccountCostRequest request, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await EnsureAccountOwnedAsync(db, request.AccountId, userId, ct);

        var cost = new AccountCost
        {
            AccountId = request.AccountId,
            Kind = request.Kind,
            Amount = request.Amount,
            PaidOn = request.PaidOn,
            Notes = request.Notes,
        };
        db.AccountCosts.Add(cost);
        await db.SaveChangesAsync(ct);
        return cost.Id;
    }

    public async Task RemoveCostAsync(Guid costId, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var cost = await db.AccountCosts.SingleOrDefaultAsync(c => c.Id == costId && c.Account!.UserId == userId, ct);
        if (cost is null) return;
        db.AccountCosts.Remove(cost);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> AddPayoutAsync(AddPayoutRequest request, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await EnsureAccountOwnedAsync(db, request.AccountId, userId, ct);

        var payout = new Payout
        {
            AccountId = request.AccountId,
            AmountRequested = request.AmountRequested,
            AmountReceived = request.AmountReceived,
            RequestedOn = request.RequestedOn,
            PaidOn = request.PaidOn,
            Status = request.Status,
            Notes = request.Notes,
        };
        db.Payouts.Add(payout);
        await db.SaveChangesAsync(ct);
        return payout.Id;
    }

    public async Task RemovePayoutAsync(Guid payoutId, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var payout = await db.Payouts.SingleOrDefaultAsync(p => p.Id == payoutId && p.Account!.UserId == userId, ct);
        if (payout is null) return;
        db.Payouts.Remove(payout);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> AddTradeAsync(CreateTradeRequest request, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await EnsureAccountOwnedAsync(db, request.AccountId, userId, ct);

        // ManualTradeFactory crea el Trade junto con sus dos Execution (entrada/salida, Source=Manual):
        // la misma entidad que usará el futuro TradeSyncService para fills reales de Tradovate/NT8.
        var trade = ManualTradeFactory.CreateManual(
            request.AccountId,
            request.Symbol,
            request.Direction,
            request.Quantity,
            request.AvgEntryPrice,
            request.AvgExitPrice,
            request.OpenedAt,
            request.ClosedAt,
            request.GrossPnL,
            request.Commissions,
            request.RiskedAmount,
            request.Tags,
            request.Notes,
            request.MaxAdverseExcursion,
            request.MaxFavorableExcursion);

        db.Trades.Add(trade); // Cascada: EF añade también las Executions colgadas de trade.Executions.
        await db.SaveChangesAsync(ct);
        return trade.Id;
    }

    public async Task DeleteTradeAsync(Guid tradeId, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var trade = await db.Trades.Include(t => t.Executions)
            .SingleOrDefaultAsync(t => t.Id == tradeId && t.Account!.UserId == userId, ct);
        if (trade is null) return;

        // Las Executions "Manual" son sintéticas: solo existen para representar este trade y se
        // borran con él. Las de fuentes reales (Tradovate/NinjaTrader) se conservan huérfanas
        // (TradeId -> null, ver DeleteBehavior.SetNull en TradeConfiguration) para que un futuro
        // TradeBuilder pueda reconstruir el trade a partir de los fills originales.
        var manualExecutions = trade.Executions.Where(e => e.Source == TradeSourceType.Manual).ToList();
        db.Executions.RemoveRange(manualExecutions);

        db.Trades.Remove(trade);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureAccountOwnedAsync(TrackRecordDbContext db, Guid accountId, string userId, CancellationToken ct)
    {
        var owned = await db.TradingAccounts.AnyAsync(a => a.Id == accountId && a.UserId == userId, ct);
        if (!owned)
        {
            throw new KeyNotFoundException($"Cuenta {accountId} no encontrada.");
        }
    }
}
