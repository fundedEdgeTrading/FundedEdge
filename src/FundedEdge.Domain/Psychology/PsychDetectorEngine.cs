using FundedEdge.Domain.Enums;

namespace FundedEdge.Domain.Psychology;

/// <summary>
/// Motor de reglas deterministas que cruza emociones auto-reportadas con datos objetivos del
/// trade (GUIA_PSICOLOGIA_TRADING.md §6.1). Cada detector es una función pura, testable con datos
/// sintéticos (un test de activación y otro de no-activación por regla).
/// </summary>
public static class PsychDetectorEngine
{
    public static IReadOnlyList<PsychInsight> Evaluate(
        IReadOnlyList<TradeWithEmotions> trades,
        IReadOnlyList<DailyMindset> checkIns)
    {
        var ordered = trades.OrderBy(t => t.OpenedAt).ToList();
        var insights = new List<PsychInsight>();

        AddIfNotNull(insights, DetectRevengeTrading(ordered));
        AddIfNotNull(insights, DetectFomoConfirmado(ordered));
        AddIfNotNull(insights, DetectEuforiaPeligrosa(ordered));
        AddIfNotNull(insights, DetectParalisis(ordered));
        AddIfNotNull(insights, DetectOvertradingEmocional(ordered));
        AddIfNotNull(insights, DetectIndisciplinaRentable(ordered));
        AddIfNotNull(insights, DetectMalaRachaEmocional(ordered, checkIns));
        AddIfNotNull(insights, DetectFatigaBurnout(ordered, checkIns));

        return insights;
    }

    private static void AddIfNotNull(List<PsychInsight> list, PsychInsight? insight)
    {
        if (insight is not null) list.Add(insight);
    }

    /// <summary>Reentrada &lt; 15 min tras un trade perdedor con tamaño ≥, o emoción Vengeful/Frustrated en la entrada.</summary>
    public static PsychInsight? DetectRevengeTrading(IReadOnlyList<TradeWithEmotions> trades)
    {
        var evidence = new List<Guid>();
        for (var i = 1; i < trades.Count; i++)
        {
            var prev = trades[i - 1];
            var current = trades[i];

            var reenteredFast = prev.IsLoss
                && current.Quantity >= prev.Quantity
                && (current.OpenedAt - prev.ClosedAt) < TimeSpan.FromMinutes(15);
            var vengefulEntry = current.HasEntryEmotion(EmotionType.Vengeful) || current.HasEntryEmotion(EmotionType.Frustrated);

            if (reenteredFast || vengefulEntry)
            {
                evidence.Add(prev.TradeId);
                evidence.Add(current.TradeId);
            }
        }

        if (evidence.Count == 0) return null;

        var episodes = evidence.Count / 2;
        return new PsychInsight(
            "revenge-trading",
            episodes >= 3 ? InsightSeverity.Critical : InsightSeverity.Warning,
            $"Revenge trading detectado ({episodes} episodio{(episodes == 1 ? "" : "s")})",
            "Tras una pérdida, espera al menos 15 minutos y confirma que el siguiente setup es válido antes de entrar — no para \"recuperar\".",
            evidence.Distinct().ToList());
    }

    /// <summary>Emoción Fomo en la entrada + entrada impulsiva + R-múltiplo negativo.</summary>
    public static PsychInsight? DetectFomoConfirmado(IReadOnlyList<TradeWithEmotions> trades)
    {
        var evidence = trades
            .Where(t => t.HasEntryEmotion(EmotionType.Fomo) && t.WasImpulsive && t.RMultiple is < 0)
            .Select(t => t.TradeId)
            .ToList();

        if (evidence.Count == 0) return null;

        return new PsychInsight(
            "fomo-confirmado",
            evidence.Count >= 3 ? InsightSeverity.Critical : InsightSeverity.Warning,
            $"FOMO confirmado en {evidence.Count} trade{(evidence.Count == 1 ? "" : "s")}",
            "Si el impulso llega con el precio ya en movimiento, espera el pullback o déjalo ir — apúntalo como trade no-tomado en vez de perseguirlo.",
            evidence);
    }

    /// <summary>Euforia/exceso de confianza tras ≥2 ganadores seguidos, con tamaño superior a la media personal.</summary>
    public static PsychInsight? DetectEuforiaPeligrosa(IReadOnlyList<TradeWithEmotions> trades)
    {
        if (trades.Count == 0) return null;
        var avgQuantity = trades.Average(t => t.Quantity);

        var evidence = new List<Guid>();
        var consecutiveWins = 0;
        foreach (var trade in trades)
        {
            if (consecutiveWins >= 2
                && (trade.HasEntryEmotion(EmotionType.Euphoric) || trade.HasEntryEmotion(EmotionType.Overconfident))
                && trade.Quantity > avgQuantity)
            {
                evidence.Add(trade.TradeId);
            }

            consecutiveWins = trade.IsWin ? consecutiveWins + 1 : 0;
        }

        if (evidence.Count == 0) return null;

        return new PsychInsight(
            "euforia-peligrosa",
            evidence.Count >= 3 ? InsightSeverity.Critical : InsightSeverity.Warning,
            $"Euforia tras racha ganadora en {evidence.Count} trade{(evidence.Count == 1 ? "" : "s")}",
            "Tras 2+ ganadores seguidos, mantén el tamaño habitual — la racha no valida un riesgo mayor, y devolverla es el patrón más común.",
            evidence);
    }

    /// <summary>Miedo/duda recurrentes junto a ganadores cortados mucho antes que los perdedores.</summary>
    public static PsychInsight? DetectParalisis(IReadOnlyList<TradeWithEmotions> trades)
    {
        var fearfulTrades = trades
            .Where(t => t.HasEmotion(EmotionType.Fearful) || t.HasEmotion(EmotionType.Doubtful))
            .ToList();

        if (fearfulTrades.Count < 3) return null;

        var wins = trades.Where(t => t.IsWin).ToList();
        var losses = trades.Where(t => t.IsLoss).ToList();
        if (wins.Count == 0 || losses.Count == 0) return null;

        var avgWinDuration = wins.Average(t => t.Duration.TotalMinutes);
        var avgLossDuration = losses.Average(t => t.Duration.TotalMinutes);

        // "Corto ganadores por miedo" — su duración media es sensiblemente menor que la de los perdedores.
        if (avgWinDuration >= avgLossDuration * 0.6) return null;

        return new PsychInsight(
            "paralisis",
            fearfulTrades.Count >= 6 ? InsightSeverity.Critical : InsightSeverity.Warning,
            "Parálisis: cortas ganadores por miedo",
            $"Tus ganadores duran de media {avgWinDuration:0} min frente a {avgLossDuration:0} min en perdedores — define un objetivo o trailing stop antes de entrar para no salir por ansiedad.",
            fearfulTrades.Select(t => t.TradeId).ToList());
    }

    /// <summary>Nº de trades del día por encima del percentil 90 personal, con valencia media del día negativa.</summary>
    public static PsychInsight? DetectOvertradingEmocional(IReadOnlyList<TradeWithEmotions> trades)
    {
        var byDay = trades.GroupBy(t => DateOnly.FromDateTime(t.OpenedAt.Date)).ToList();
        if (byDay.Count < 5) return null; // muestra insuficiente para un percentil fiable

        var counts = byDay.Select(g => g.Count()).OrderBy(c => c).ToList();
        var p90 = Percentile(counts, 0.90);

        var evidence = new List<Guid>();
        foreach (var day in byDay)
        {
            if (day.Count() <= p90) continue;

            var valences = day.SelectMany(t => t.EntryEmotions.Concat(t.ExitEmotions))
                .Select(e => EmotionProfile.Valence(e.Emotion))
                .ToList();
            if (valences.Count == 0) continue;

            if (valences.Average() < 0)
            {
                evidence.AddRange(day.Select(t => t.TradeId));
            }
        }

        if (evidence.Count == 0) return null;

        return new PsychInsight(
            "overtrading-emocional",
            InsightSeverity.Warning,
            "Overtrading emocional",
            "En tus días con más operativa de lo habitual, la emoción dominante es negativa — probablemente aburrimiento o frustración. Fija un máximo de trades/día y párate al llegar a él.",
            evidence);
    }

    /// <summary>Sin plan o entrada impulsiva, pero con resultado positivo — el refuerzo más tóxico.</summary>
    public static PsychInsight? DetectIndisciplinaRentable(IReadOnlyList<TradeWithEmotions> trades)
    {
        var evidence = trades
            .Where(t => (t.Adherence == PlanAdherence.NoPlan || t.WasImpulsive) && t.NetPnL > 0)
            .Select(t => t.TradeId)
            .ToList();

        if (evidence.Count == 0) return null;

        return new PsychInsight(
            "indisciplina-rentable",
            InsightSeverity.Info,
            $"Indisciplina rentable en {evidence.Count} trade{(evidence.Count == 1 ? "" : "s")}",
            "Ganaste sin plan o de forma impulsiva — el refuerzo más peligroso, porque premia el mal proceso. Revisa si el resultado se sostendría con más muestra.",
            evidence);
    }

    /// <summary>Valencia media móvil (7 días) negativa y descendente, o ≥3 días con emociones de alta activación negativa.</summary>
    public static PsychInsight? DetectMalaRachaEmocional(IReadOnlyList<TradeWithEmotions> trades, IReadOnlyList<DailyMindset> checkIns)
    {
        var dailyValence = DailyValence(trades, checkIns);
        if (dailyValence.Count < 7) return null;

        var ordered = dailyValence.OrderBy(d => d.Date).ToList();
        var last7 = ordered.TakeLast(7).ToList();
        var prev7 = ordered.Count >= 14 ? ordered.SkipLast(7).TakeLast(7).ToList() : null;

        var last7Avg = last7.Average(d => d.Valence);
        var descending = prev7 is not null && last7Avg < prev7.Average(d => d.Valence);

        var highActivationNegativeDays = trades
            .GroupBy(t => DateOnly.FromDateTime(t.OpenedAt.Date))
            .Count(g => g.SelectMany(t => t.EntryEmotions.Concat(t.ExitEmotions))
                .Any(e => EmotionProfile.IsHighActivationNegative(e.Emotion)));

        var streakActive = (last7Avg < 0 && descending) || highActivationNegativeDays >= 3;
        if (!streakActive) return null;

        var evidence = trades
            .Where(t => last7.Select(d => d.Date).Contains(DateOnly.FromDateTime(t.OpenedAt.Date)))
            .Select(t => t.TradeId)
            .ToList();

        return new PsychInsight(
            "mala-racha-emocional",
            InsightSeverity.Critical,
            "Mala racha emocional activa",
            "Reduce el tamaño al 50%, limita el número de trades del día y considera un día de descanso si no mejora — activa el protocolo de racha emocional.",
            evidence);
    }

    /// <summary>Desconexión recurrente + sueño bajo + rendimiento decreciente.</summary>
    public static PsychInsight? DetectFatigaBurnout(IReadOnlyList<TradeWithEmotions> trades, IReadOnlyList<DailyMindset> checkIns)
    {
        var tiredDays = checkIns
            .Where(c => c.DominantPreMarketEmotion == EmotionType.Detached && c.SleepQuality <= 2)
            .ToList();

        if (tiredDays.Count < 3) return null;

        var ordered = trades.OrderBy(t => t.OpenedAt).ToList();
        if (ordered.Count < 6) return null;

        var half = ordered.Count / 2;
        var firstHalfAvg = ordered.Take(half).Average(t => t.NetPnL);
        var secondHalfAvg = ordered.Skip(half).Average(t => t.NetPnL);

        if (secondHalfAvg >= firstHalfAvg) return null;

        var tiredDates = tiredDays.Select(c => c.Date).ToHashSet();
        var evidence = ordered
            .Where(t => tiredDates.Contains(DateOnly.FromDateTime(t.OpenedAt.Date)))
            .Select(t => t.TradeId)
            .ToList();

        return new PsychInsight(
            "fatiga-burnout",
            InsightSeverity.Warning,
            "Señales de fatiga/burnout",
            "Sueño bajo y desconexión repetidos coinciden con rendimiento decreciente. Prioriza descanso, no ajustes técnicos: un día sin operar puede ser la mejora de mayor impacto ahora mismo.",
            evidence);
    }

    /// <summary>Valencia media diaria combinando emociones de trades (entrada/salida) y check-in diario.</summary>
    public static List<(DateOnly Date, double Valence)> DailyValence(
        IReadOnlyList<TradeWithEmotions> trades, IReadOnlyList<DailyMindset> checkIns)
    {
        var byDay = trades.GroupBy(t => DateOnly.FromDateTime(t.OpenedAt.Date))
            .ToDictionary(g => g.Key, g => g.SelectMany(t => t.EntryEmotions.Concat(t.ExitEmotions)).Select(e => (double)EmotionProfile.Valence(e.Emotion)).ToList());

        foreach (var checkIn in checkIns)
        {
            var valence = (double)EmotionProfile.Valence(checkIn.DominantPreMarketEmotion);
            if (!byDay.TryGetValue(checkIn.Date, out var list))
            {
                list = [];
                byDay[checkIn.Date] = list;
            }
            list.Add(valence);
        }

        return byDay
            .Where(kv => kv.Value.Count > 0)
            .Select(kv => (kv.Key, kv.Value.Average()))
            .ToList();
    }

    internal static double Percentile(IReadOnlyList<int> sortedValues, double percentile)
    {
        if (sortedValues.Count == 1) return sortedValues[0];
        var rank = percentile * (sortedValues.Count - 1);
        var lower = (int)Math.Floor(rank);
        var upper = (int)Math.Ceiling(rank);
        if (lower == upper) return sortedValues[lower];
        return sortedValues[lower] + (rank - lower) * (sortedValues[upper] - sortedValues[lower]);
    }
}
