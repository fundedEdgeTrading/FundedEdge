using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Trades;

/// <summary>
/// Reconstruye Trades (round-turns cerrados) a partir de una colección de Executions de UNA
/// cuenta, agrupando internamente por símbolo y procesando cada grupo en orden cronológico con
/// seguimiento de posición neta (FIFO). Asigna Execution.TradeId/Trade a las Trades resultantes;
/// NO persiste nada — orquestar el guardado (borrar Trades obsoletas, guardar las nuevas) es
/// responsabilidad del llamador (ver TradeSyncOrchestrator).
///
/// Solo debe alimentarse con Executions NO manuales (Source != Manual): los trades manuales ya
/// vienen completos de <see cref="ManualTradeFactory"/> y no pasan por este builder.
///
/// Modelo de PnL: "round-turn con precio medio ponderado" — cada Trade acumula un precio medio
/// de entrada y uno de salida a lo largo de toda su vida (desde que la posición sale de cero
/// hasta que vuelve a cero), en vez de emparejar lote a lote. Es una simplificación deliberada
/// habitual en journals de trading: el PnL total del round-turn es idéntico a un cálculo FIFO
/// estricto lote a lote cuando se agrega todo el round-turn, y es muchísimo más simple de
/// mantener y testear.
///
/// Simplificación documentada para "flips" (un único fill que cierra la posición existente Y
/// abre una nueva en sentido contrario): el fill se asigna a la Trade que CIERRA (por
/// convención); la nueva Trade que abre queda con su AvgEntryPrice/Quantity correctos pero sin
/// ese fill enlazado en su propia colección de Executions — el modelo de FK único de
/// Execution.TradeId no permite que una misma fila pertenezca a dos Trades a la vez.
/// </summary>
public static class TradeBuilder
{
    public static IReadOnlyList<Trade> Build(IEnumerable<Execution> executions, IReadOnlyCollection<Instrument> instruments)
    {
        var closedTrades = new List<Trade>();

        var bySymbol = executions.GroupBy(e => e.Symbol, StringComparer.OrdinalIgnoreCase);

        foreach (var group in bySymbol)
        {
            var ordered = group
                .OrderBy(e => e.ExecutedAt)
                .ThenBy(e => e.ExternalId, StringComparer.Ordinal) // desempate determinista para fills al mismo instante
                .ToList();

            if (ordered.Count == 0) continue;

            var accountId = ordered[0].AccountId;
            var symbol = ordered[0].Symbol;
            var instrument = ResolveInstrument(symbol, instruments);

            closedTrades.AddRange(BuildForSymbol(accountId, symbol, ordered, instrument));
        }

        return closedTrades;
    }

    private static IReadOnlyList<Trade> BuildForSymbol(Guid accountId, string symbol, IReadOnlyList<Execution> ordered, Instrument? instrument)
    {
        var result = new List<Trade>();

        Trade? current = null;
        long positionQty = 0; // signed: positivo = long, negativo = short
        decimal entryPriceQtySum = 0m;
        long entryQtySum = 0;
        decimal exitPriceQtySum = 0m;
        long exitQtySum = 0;

        foreach (var exec in ordered)
        {
            var signedQty = exec.Side == OrderSide.Buy ? exec.Quantity : -exec.Quantity;

            if (current is null)
            {
                // Abre una nueva Trade: la posición partía de cero.
                current = new Trade
                {
                    AccountId = accountId,
                    InstrumentId = instrument?.Id,
                    Symbol = symbol,
                    Direction = signedQty > 0 ? TradeDirection.Long : TradeDirection.Short,
                    OpenedAt = exec.ExecutedAt,
                };
                entryPriceQtySum = exec.Price * Math.Abs(signedQty);
                entryQtySum = Math.Abs(signedQty);
                exitPriceQtySum = 0m;
                exitQtySum = 0;
                positionQty = signedQty;

                LinkExecution(current, exec);
                continue;
            }

            var sameDirection = Math.Sign(positionQty) == Math.Sign(signedQty);

            if (sameDirection)
            {
                // Escalado de entrada: amplía la posición en el mismo sentido.
                entryPriceQtySum += exec.Price * Math.Abs(signedQty);
                entryQtySum += Math.Abs(signedQty);
                positionQty += signedQty;

                LinkExecution(current, exec);
                continue;
            }

            // Fill en sentido contrario: puede ser reducción parcial, cierre exacto o cierre+flip.
            var closingQty = Math.Min(Math.Abs(positionQty), Math.Abs(signedQty));
            exitPriceQtySum += exec.Price * closingQty;
            exitQtySum += closingQty;

            var newPositionQty = positionQty + signedQty;

            LinkExecution(current, exec); // El fill de reducción/cierre/flip siempre se enlaza a la Trade que se está cerrando.

            if (newPositionQty == 0)
            {
                // Cierre exacto: la posición vuelve a cero.
                result.Add(FinalizeTrade(current, exec.ExecutedAt, entryPriceQtySum, entryQtySum, exitPriceQtySum, exitQtySum, instrument));
                current = null;
                positionQty = 0;
            }
            else if (Math.Sign(newPositionQty) == Math.Sign(positionQty))
            {
                // Reducción parcial: la posición se reduce pero no cruza cero. La Trade sigue abierta.
                positionQty = newPositionQty;
            }
            else
            {
                // Flip: este fill cierra la posición actual y abre una nueva en sentido contrario.
                result.Add(FinalizeTrade(current, exec.ExecutedAt, entryPriceQtySum, entryQtySum, exitPriceQtySum, exitQtySum, instrument));

                var leftoverQty = Math.Abs(newPositionQty);
                current = new Trade
                {
                    AccountId = accountId,
                    InstrumentId = instrument?.Id,
                    Symbol = symbol,
                    Direction = newPositionQty > 0 ? TradeDirection.Long : TradeDirection.Short,
                    OpenedAt = exec.ExecutedAt,
                };
                entryPriceQtySum = exec.Price * leftoverQty;
                entryQtySum = leftoverQty;
                exitPriceQtySum = 0m;
                exitQtySum = 0;
                positionQty = newPositionQty;
                // El fill que originó el flip no se enlaza a esta nueva Trade (ver comentario de
                // clase); su precio y cantidad ya han quedado reflejados en AvgEntryPrice/Quantity
                // cuando esta Trade se finalice más adelante.
            }
        }

        // Si la posición queda abierta (no vuelve a cero), la Trade en curso NUNCA se añade a
        // result y por tanto nunca se persiste — hay que deshacer la asignación tentativa de
        // TradeId/Trade que LinkExecution fue dejando en sus Executions según se procesaban,
        // o quedarían apuntando a un Trade.Id fantasma que no existe en la base de datos.
        if (current is not null)
        {
            foreach (var exec in current.Executions)
            {
                exec.TradeId = null;
                exec.Trade = null;
            }
        }

        return result;
    }

    private static void LinkExecution(Trade trade, Execution exec)
    {
        exec.TradeId = trade.Id;
        exec.Trade = trade;
        trade.Executions.Add(exec);
    }

    private static Trade FinalizeTrade(
        Trade trade,
        DateTimeOffset closedAt,
        decimal entryPriceQtySum,
        long entryQtySum,
        decimal exitPriceQtySum,
        long exitQtySum,
        Instrument? instrument)
    {
        var avgEntry = entryQtySum > 0 ? entryPriceQtySum / entryQtySum : 0m;
        var avgExit = exitQtySum > 0 ? exitPriceQtySum / exitQtySum : 0m;

        trade.Quantity = (int)entryQtySum;
        trade.AvgEntryPrice = avgEntry;
        trade.AvgExitPrice = avgExit;
        trade.ClosedAt = closedAt;
        trade.GrossPnL = ComputeGrossPnL(trade.Direction, avgEntry, avgExit, entryQtySum, instrument);
        trade.Commissions = trade.Executions.Sum(e => e.Commission);

        return trade;
    }

    private static decimal ComputeGrossPnL(TradeDirection direction, decimal avgEntry, decimal avgExit, long quantity, Instrument? instrument)
    {
        // Sin instrumento resuelto, se usa tickSize=1/tickValue=1 (PnL = diferencia de precio en
        // bruto). Es un fallback deliberado para símbolos desconocidos; el llamador puede
        // detectarlo por InstrumentId == null en la Trade resultante.
        var tickSize = instrument?.TickSize ?? 1m;
        var tickValue = instrument?.TickValue ?? 1m;
        var priceDiff = avgExit - avgEntry;
        var directionSign = direction == TradeDirection.Long ? 1 : -1;

        return priceDiff / tickSize * tickValue * quantity * directionSign;
    }

    /// <summary>
    /// Resuelve el símbolo de contrato (p.ej. "MESZ5") al Instrument cuyo Root es el prefijo
    /// más largo que coincide (para que "MES" gane a "ES" en símbolos que empezaran por ambos).
    /// </summary>
    private static Instrument? ResolveInstrument(string symbol, IReadOnlyCollection<Instrument> instruments) =>
        instruments
            .Where(i => symbol.StartsWith(i.Root, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(i => i.Root.Length)
            .FirstOrDefault();
}
