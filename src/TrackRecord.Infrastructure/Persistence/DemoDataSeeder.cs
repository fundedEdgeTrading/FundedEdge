using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Domain.Trades;
using TrackRecord.Infrastructure.Identity;

namespace TrackRecord.Infrastructure.Persistence;

/// <summary>
/// Batería de datos de demostración: crea un usuario de prueba con 6 cuentas de fondeo en
/// distintos puntos del ciclo de vida (evaluación en curso, reset, fondeada con payouts,
/// suspendida, retirada…), con sus trades, costes, transiciones, payouts y registros de
/// psicología — todo determinista (Random con semilla fija) para que cada arranque genere
/// los mismos datos.
///
/// Se activa con la clave de configuración "Database:SeedDemo" = "true" (activada por defecto
/// en appsettings.Development.json, nunca en producción). Idempotente: si el usuario demo ya
/// existe no hace nada.
///
/// Credenciales del usuario demo (solo para entornos de desarrollo):
///   email    demo@fundededge.test
///   password FundedEdge!Demo2026
/// </summary>
public class DemoDataSeeder(
    UserManager<ApplicationUser> userManager,
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    ILogger<DemoDataSeeder> logger)
{
    public const string DemoEmail = "demo@fundededge.test";
    public const string DemoPassword = "FundedEdge!Demo2026";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(DemoEmail) is not null)
        {
            logger.LogDebug("Usuario demo ya existente; se omite el seed de demostración.");
            return;
        }

        var user = new ApplicationUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true, // sin este flag el login queda bloqueado (RequireConfirmedAccount)
            DisplayName = "Trader Demo",
            PlanTier = PlanTier.Elite, // sin límite de cuentas activas, módulos completos
        };

        var created = await userManager.CreateAsync(user, DemoPassword);
        if (!created.Succeeded)
        {
            logger.LogWarning("No se pudo crear el usuario demo: {Errors}",
                string.Join("; ", created.Errors.Select(e => e.Description)));
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.Today);

        // ── 1 · Apex 100K fondeada y rentable: el caso "todo va bien" (2 payouts + 1 pendiente) ──
        var apex100 = NewAccount(user.Id, SeedData.ApexId, Guid.Parse("b0000000-0000-0000-0000-000000000002"),
            "Apex 100K #1", 100_000m, 6_000m, 3_000m, DrawdownType.Trailing, DataFeedType.NinjaTrader, today.AddDays(-120));
        apex100.TransitionTo(AccountStage.Funded, At(today.AddDays(-88)), "Evaluación superada en 32 días");
        apex100.Costs.Add(Cost(CostKind.Evaluation, 207m, today.AddDays(-120)));
        apex100.Costs.Add(Cost(CostKind.Evaluation, 207m, today.AddDays(-90), "Mensualidad de la evaluación (2º mes)"));
        apex100.Costs.Add(Cost(CostKind.Activation, 130m, today.AddDays(-88)));
        apex100.Costs.Add(Cost(CostKind.DataFee, 39m, today.AddDays(-60), "Datos CME (mensual)"));
        apex100.Costs.Add(Cost(CostKind.DataFee, 39m, today.AddDays(-30), "Datos CME (mensual)"));
        apex100.Payouts.Add(Payout(2_000m, 2_000m, today.AddDays(-55), today.AddDays(-52), PayoutStatus.Paid));
        apex100.Payouts.Add(Payout(2_500m, 2_500m, today.AddDays(-25), today.AddDays(-22), PayoutStatus.Paid));
        apex100.Payouts.Add(Payout(1_800m, 0m, today.AddDays(-3), null, PayoutStatus.Requested));
        db.TradingAccounts.Add(apex100);
        var apex100Trades = AddTrades(db, apex100, seed: 1, count: 85, firstDay: today.AddDays(-118), lastDay: today.AddDays(-1),
            winRate: 0.58, avgWinPoints: 22m, avgLossPoints: 14m, source: TradeSourceType.CsvImport, sourceLabel: "NinjaTrader 8");

        // ── 2 · Apex 50K en evaluación con un reset por el camino ──────────────────────────────
        var apex50 = NewAccount(user.Id, SeedData.ApexId, Guid.Parse("b0000000-0000-0000-0000-000000000001"),
            "Apex 50K #2", 50_000m, 3_000m, 2_500m, DrawdownType.Trailing, DataFeedType.NinjaTrader, today.AddDays(-45));
        apex50.Events.Add(new AccountEvent
        {
            AccountId = apex50.Id,
            FromStage = AccountStage.Evaluation,
            ToStage = AccountStage.Evaluation,
            OccurredAt = At(today.AddDays(-20)),
            Notes = "Reset: superado el trailing drawdown tras 3 días en rojo",
        });
        apex50.Costs.Add(Cost(CostKind.Evaluation, 167m, today.AddDays(-45)));
        apex50.Costs.Add(Cost(CostKind.Reset, 85m, today.AddDays(-20)));
        db.TradingAccounts.Add(apex50);
        AddTrades(db, apex50, seed: 2, count: 30, firstDay: today.AddDays(-44), lastDay: today.AddDays(-1),
            winRate: 0.48, avgWinPoints: 18m, avgLossPoints: 15m, source: TradeSourceType.Manual, sourceLabel: null);

        // ── 3 · Topstep 100K suspendida: evaluación fallida por drawdown ───────────────────────
        var topstep = NewAccount(user.Id, SeedData.TopstepId, Guid.Parse("b0000000-0000-0000-0000-000000000008"),
            "Topstep 100K", 100_000m, 6_000m, 3_000m, DrawdownType.Trailing, DataFeedType.Tradovate, today.AddDays(-100));
        topstep.TransitionTo(AccountStage.Failed, At(today.AddDays(-72)), "Trailing drawdown superado en una racha de 5 pérdidas");
        topstep.Costs.Add(Cost(CostKind.Evaluation, 245m, today.AddDays(-100)));
        db.TradingAccounts.Add(topstep);
        AddTrades(db, topstep, seed: 3, count: 22, firstDay: today.AddDays(-99), lastDay: today.AddDays(-72),
            winRate: 0.32, avgWinPoints: 15m, avgLossPoints: 20m, source: TradeSourceType.CsvImport, sourceLabel: "Tradovate");

        // ── 4 · Tradeify 50K fondeada con payout pagado y otro solicitado ──────────────────────
        var tradeify = NewAccount(user.Id, SeedData.TradeifyId, Guid.Parse("b0000000-0000-0000-0000-000000000003"),
            "Tradeify Growth 50K", 50_000m, 3_000m, 2_000m, DrawdownType.EndOfDay, DataFeedType.Tradovate, today.AddDays(-80));
        tradeify.TransitionTo(AccountStage.Funded, At(today.AddDays(-42)), "Objetivo alcanzado con regla de consistencia holgada");
        tradeify.Costs.Add(Cost(CostKind.Evaluation, 165m, today.AddDays(-80)));
        tradeify.Payouts.Add(Payout(1_200m, 1_080m, today.AddDays(-15), today.AddDays(-12), PayoutStatus.Paid, "Split 90/10"));
        tradeify.Payouts.Add(Payout(900m, 0m, today.AddDays(-1), null, PayoutStatus.Requested));
        db.TradingAccounts.Add(tradeify);
        AddTrades(db, tradeify, seed: 4, count: 55, firstDay: today.AddDays(-78), lastDay: today.AddDays(-1),
            winRate: 0.55, avgWinPoints: 16m, avgLossPoints: 11m, source: TradeSourceType.CsvImport, sourceLabel: "Tradovate");

        // ── 5 · Lucid 50K recién comprada: evaluación en sus primeros días ─────────────────────
        var lucid = NewAccount(user.Id, SeedData.LucidTradingId, Guid.Parse("b0000000-0000-0000-0000-000000000005"),
            "Lucid 50K", 50_000m, 2_000m, 2_000m, DrawdownType.Static, DataFeedType.Tradovate, today.AddDays(-12));
        lucid.Costs.Add(Cost(CostKind.Evaluation, 137m, today.AddDays(-12)));
        db.TradingAccounts.Add(lucid);
        AddTrades(db, lucid, seed: 5, count: 9, firstDay: today.AddDays(-11), lastDay: today.AddDays(-1),
            winRate: 0.62, avgWinPoints: 14m, avgLossPoints: 10m, source: TradeSourceType.Manual, sourceLabel: null);

        // ── 6 · TPT 25K: fondeada, 2 payouts y retirada voluntaria (ciclo completo) ────────────
        var tpt = NewAccount(user.Id, SeedData.TakeProfitTraderId, Guid.Parse("b0000000-0000-0000-0000-000000000024"),
            "TPT 25K", 25_000m, 1_500m, 1_500m, DrawdownType.EndOfDay, DataFeedType.NinjaTrader, today.AddDays(-150));
        tpt.TransitionTo(AccountStage.Funded, At(today.AddDays(-112)), "Evaluación superada");
        tpt.TransitionTo(AccountStage.Withdrawn, At(today.AddDays(-21)), "Cuenta cerrada tras retirar beneficios: foco en las cuentas grandes");
        tpt.Costs.Add(Cost(CostKind.Evaluation, 150m, today.AddDays(-150)));
        tpt.Costs.Add(Cost(CostKind.Activation, 130m, today.AddDays(-112)));
        tpt.Payouts.Add(Payout(800m, 640m, today.AddDays(-70), today.AddDays(-67), PayoutStatus.Paid, "Split 80/20"));
        tpt.Payouts.Add(Payout(1_000m, 800m, today.AddDays(-25), today.AddDays(-22), PayoutStatus.Paid, "Split 80/20"));
        db.TradingAccounts.Add(tpt);
        AddTrades(db, tpt, seed: 6, count: 48, firstDay: today.AddDays(-148), lastDay: today.AddDays(-23),
            winRate: 0.54, avgWinPoints: 12m, avgLossPoints: 9m, source: TradeSourceType.CsvImport, sourceLabel: "NinjaTrader 8");

        // ── Psicología: check-ins de los últimos 14 días y diario emocional de trades recientes ─
        SeedPsychology(db, user.Id, today, apex100Trades);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Datos de demostración creados para {Email} (6 cuentas).", DemoEmail);
    }

    private static TradingAccount NewAccount(
        string userId, Guid firmId, Guid programId, string name, decimal size, decimal target,
        decimal maxDd, DrawdownType ddType, DataFeedType feed, DateOnly purchasedOn) => new()
    {
        UserId = userId,
        PropFirmId = firmId,
        EvaluationProgramId = programId,
        DisplayName = name,
        AccountSize = size,
        ProfitTarget = target,
        MaxDrawdown = maxDd,
        DrawdownType = ddType,
        Stage = AccountStage.Evaluation,
        Feed = feed,
        PurchasedOn = purchasedOn,
    };

    private static AccountCost Cost(CostKind kind, decimal amount, DateOnly paidOn, string? notes = null) =>
        new() { Kind = kind, Amount = amount, PaidOn = paidOn, Notes = notes };

    private static Payout Payout(decimal requested, decimal received, DateOnly requestedOn, DateOnly? paidOn, PayoutStatus status, string? notes = null) =>
        new() { AmountRequested = requested, AmountReceived = received, RequestedOn = requestedOn, PaidOn = paidOn, Status = status, Notes = notes };

    private static DateTimeOffset At(DateOnly day) => new(day.ToDateTime(new TimeOnly(17, 0)), TimeSpan.Zero);

    /// <summary>
    /// Genera trades deterministas repartidos en días laborables del rango, con la winrate y el
    /// tamaño medio de ganancia/pérdida indicados (en puntos del instrumento). Los precios de
    /// entrada/salida son coherentes con el P&L y el multiplicador de cada contrato.
    /// </summary>
    private static List<Trade> AddTrades(
        TrackRecordDbContext db, TradingAccount account, int seed, int count,
        DateOnly firstDay, DateOnly lastDay, double winRate, decimal avgWinPoints, decimal avgLossPoints,
        TradeSourceType source, string? sourceLabel)
    {
        // (símbolo, multiplicador $/punto, precio base). Micros para tamaños pequeños.
        (string Symbol, decimal Multiplier, decimal BasePrice)[] instruments =
        [
            ("MES", 5m, 6200m),
            ("MNQ", 2m, 22600m),
            ("ES", 50m, 6200m),
            ("MGC", 10m, 3350m),
        ];
        string[] tagPool = ["breakout", "pullback", "orb", "news", "reversion"];

        var random = new Random(seed);
        var trades = new List<Trade>(count);
        var totalDays = lastDay.DayNumber - firstDay.DayNumber;

        for (var i = 0; i < count; i++)
        {
            // Día laborable pseudoaleatorio dentro del rango, con orden creciente aproximado.
            var day = firstDay.AddDays((int)(totalDays * (i / (double)count))).AddDays(random.Next(0, 2));
            if (day.DayOfWeek is DayOfWeek.Saturday) day = day.AddDays(2);
            if (day.DayOfWeek is DayOfWeek.Sunday) day = day.AddDays(1);
            if (day > lastDay) day = lastDay;

            var inst = instruments[random.Next(0, account.AccountSize >= 100_000m ? instruments.Length : 2)];
            var direction = random.NextDouble() < 0.5 ? TradeDirection.Long : TradeDirection.Short;
            var qty = random.Next(1, account.AccountSize >= 100_000m ? 5 : 3);
            var isWin = random.NextDouble() < winRate;

            // Puntos ganados/perdidos con variación ±60% sobre la media.
            var basePoints = isWin ? avgWinPoints : -avgLossPoints;
            var points = Math.Round(basePoints * (decimal)(0.4 + random.NextDouble() * 1.2) * 4m, 0) / 4m; // rejilla de 0.25
            if (points == 0m) points = isWin ? 0.25m : -0.25m;

            var entryPrice = inst.BasePrice + Math.Round((decimal)(random.NextDouble() * 80 - 40) * 4m, 0) / 4m;
            var exitPrice = direction == TradeDirection.Long ? entryPrice + points : entryPrice - points;
            var grossPnL = points * inst.Multiplier * qty;
            var commission = Math.Round(1.24m * qty * 2m, 2); // ida y vuelta por contrato

            var openedAt = new DateTimeOffset(day.ToDateTime(new TimeOnly(9 + random.Next(0, 6), random.Next(0, 60))), TimeSpan.Zero);
            var closedAt = openedAt.AddMinutes(random.Next(4, 95));

            // MAE/MFE plausibles: la pérdida flotante nunca es menor que la pérdida final.
            var riskDollars = Math.Round(avgLossPoints * inst.Multiplier * qty, 0);
            decimal? mae = Math.Round(Math.Max(isWin ? 0.3m * riskDollars : -grossPnL, (decimal)random.NextDouble() * riskDollars), 0);
            decimal? mfe = Math.Round(Math.Max(isWin ? grossPnL : 0.2m * riskDollars, 0m) + (decimal)random.NextDouble() * 25m, 0);
            if (random.NextDouble() < 0.25) { mae = null; mfe = null; } // no siempre se registra

            var trade = ManualTradeFactory.Create(
                account.Id,
                inst.Symbol,
                direction,
                qty,
                entryPrice,
                exitPrice,
                openedAt,
                closedAt,
                grossPnL,
                commission,
                riskedAmount: riskDollars,
                tags: random.NextDouble() < 0.7 ? tagPool[random.Next(tagPool.Length)] : null,
                notes: sourceLabel is null ? null : $"Importado de CSV ({sourceLabel})",
                source,
                $"demo-{seed}-{i}-entry",
                $"demo-{seed}-{i}-exit",
                mae,
                mfe);

            db.Trades.Add(trade);
            trades.Add(trade);
        }

        return trades;
    }

    private static void SeedPsychology(TrackRecordDbContext db, string userId, DateOnly today, List<Trade> recentTrades)
    {
        var random = new Random(42);
        EmotionType[] preMarket = [EmotionType.Calm, EmotionType.Confident, EmotionType.Anxious, EmotionType.Hopeful, EmotionType.Doubtful];

        for (var d = 14; d >= 1; d--)
        {
            var day = today.AddDays(-d);
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

            db.DailyMindsetCheckIns.Add(new DailyMindsetCheckIn
            {
                UserId = userId,
                Date = day,
                SleepQuality = random.Next(2, 6),
                ExternalStress = random.Next(1, 5),
                PreMarketFocus = random.Next(2, 6),
                DominantPreMarketEmotion = preMarket[random.Next(preMarket.Length)],
                Note = d % 5 == 0 ? "Sesión asiática movida; abrir con tamaño reducido." : null,
            });
        }

        // Diario emocional de los últimos trades de la cuenta principal: antes/después por trade.
        foreach (var trade in recentTrades.OrderByDescending(t => t.ClosedAt).Take(12))
        {
            var win = trade.GrossPnL - trade.Commissions >= 0;
            var adherence = random.NextDouble() < 0.75 ? PlanAdherence.FollowedPlan : PlanAdherence.PartialDeviation;
            var impulsive = !win && random.NextDouble() < 0.35;

            db.TradeEmotionLogs.Add(new TradeEmotionLog
            {
                TradeId = trade.Id,
                Moment = EmotionMoment.BeforeEntry,
                Emotion = impulsive ? EmotionType.Fomo : EmotionType.Confident,
                Intensity = random.Next(2, 6),
                Adherence = adherence,
                WasImpulsive = impulsive,
                LoggedAt = trade.ClosedAt.AddMinutes(30),
            });
            db.TradeEmotionLogs.Add(new TradeEmotionLog
            {
                TradeId = trade.Id,
                Moment = EmotionMoment.AfterExit,
                Emotion = win ? EmotionType.Calm : EmotionType.Frustrated,
                Intensity = random.Next(2, 6),
                Adherence = adherence,
                WasImpulsive = impulsive,
                LoggedAt = trade.ClosedAt.AddMinutes(35),
            });
        }
    }
}
