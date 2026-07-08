using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Domain.Trades;

namespace TrackRecord.Domain.Tests;

public class TradeBuilderTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    private static readonly Instrument Es = new()
    {
        Id = Guid.NewGuid(),
        Root = "ES",
        Name = "E-mini S&P 500",
        TickSize = 0.25m,
        TickValue = 12.50m,
    };

    private static readonly Instrument Mes = new()
    {
        Id = Guid.NewGuid(),
        Root = "MES",
        Name = "Micro E-mini S&P 500",
        TickSize = 0.25m,
        TickValue = 1.25m,
    };

    private static Execution Fill(string externalId, string symbol, OrderSide side, int qty, decimal price, DateTimeOffset at, decimal commission = 0m) =>
        new()
        {
            AccountId = AccountId,
            ExternalId = externalId,
            Source = TradeSourceType.Tradovate,
            Symbol = symbol,
            Side = side,
            Quantity = qty,
            Price = price,
            ExecutedAt = at,
            Commission = commission,
        };

    private static DateTimeOffset T(int minute) => new(2026, 1, 5, 14, minute, 0, TimeSpan.Zero);

    // ---- Caso base: una entrada, una salida ----

    [Fact]
    public void Build_SingleEntrySingleExit_ProducesOneClosedTradeWithCorrectPnL()
    {
        var fills = new[]
        {
            Fill("f1", "ESH6", OrderSide.Buy, 1, 5000m, T(0)),
            Fill("f2", "ESH6", OrderSide.Sell, 1, 5010m, T(5)),
        };

        var trades = TradeBuilder.Build(fills, [Es]);

        var trade = Assert.Single(trades);
        Assert.Equal(TradeDirection.Long, trade.Direction);
        Assert.Equal(1, trade.Quantity);
        Assert.Equal(5000m, trade.AvgEntryPrice);
        Assert.Equal(5010m, trade.AvgExitPrice);
        Assert.Equal(500m, trade.GrossPnL); // 10 puntos * (12.50 / 0.25) $/punto * 1 contrato
        Assert.Equal(2, trade.Executions.Count);
        Assert.All(trade.Executions, e => Assert.Equal(trade.Id, e.TradeId));
    }

    [Fact]
    public void Build_ShortTrade_ProfitsWhenPriceDrops()
    {
        var fills = new[]
        {
            Fill("f1", "ESH6", OrderSide.Sell, 1, 5000m, T(0)),
            Fill("f2", "ESH6", OrderSide.Buy, 1, 4990m, T(5)),
        };

        var trade = Assert.Single(TradeBuilder.Build(fills, [Es]));

        Assert.Equal(TradeDirection.Short, trade.Direction);
        Assert.Equal(500m, trade.GrossPnL); // baja 10 puntos a favor del corto
    }

    // ---- Escalados ----

    [Fact]
    public void Build_ScalingIn_UsesWeightedAverageEntryPrice()
    {
        var fills = new[]
        {
            Fill("f1", "ESH6", OrderSide.Buy, 1, 5000m, T(0)),
            Fill("f2", "ESH6", OrderSide.Buy, 1, 5010m, T(1)),
            Fill("f3", "ESH6", OrderSide.Sell, 2, 5020m, T(5)),
        };

        var trade = Assert.Single(TradeBuilder.Build(fills, [Es]));

        Assert.Equal(2, trade.Quantity);
        Assert.Equal(5005m, trade.AvgEntryPrice); // (5000+5010)/2
        Assert.Equal(5020m, trade.AvgExitPrice);
        Assert.Equal(1500m, trade.GrossPnL); // (5020-5005) * 2 * 50
    }

    [Fact]
    public void Build_ScalingOut_PartialCloseKeepsTradeOpenUntilPositionIsFlat()
    {
        var fills = new[]
        {
            Fill("f1", "ESH6", OrderSide.Buy, 2, 5000m, T(0)),
            Fill("f2", "ESH6", OrderSide.Sell, 1, 5010m, T(1)),  // reducción parcial: no cierra
            Fill("f3", "ESH6", OrderSide.Sell, 1, 5020m, T(5)),  // cierra del todo
        };

        var trade = Assert.Single(TradeBuilder.Build(fills, [Es]));

        Assert.Equal(2, trade.Quantity);
        Assert.Equal(5000m, trade.AvgEntryPrice);
        Assert.Equal(5015m, trade.AvgExitPrice); // (5010*1 + 5020*1) / 2
        Assert.Equal(1500m, trade.GrossPnL); // (5015-5000) * 2 * 50
        Assert.Equal(3, trade.Executions.Count);
    }

    [Fact]
    public void Build_ScaleBackInAfterPartialReduce_StillNetsToCorrectQuantityAndPnL()
    {
        // Long 2 -> vende 1 (reduce a long 1) -> vuelve a comprar 2 (long 3) -> cierra vendiendo 3.
        var fills = new[]
        {
            Fill("f1", "ESH6", OrderSide.Buy, 2, 5000m, T(0)),
            Fill("f2", "ESH6", OrderSide.Sell, 1, 5010m, T(1)),
            Fill("f3", "ESH6", OrderSide.Buy, 2, 5000m, T(2)),
            Fill("f4", "ESH6", OrderSide.Sell, 3, 5010m, T(3)),
        };

        var trade = Assert.Single(TradeBuilder.Build(fills, [Es]));

        Assert.Equal(4, trade.Quantity); // entradas: 2 + 2 = 4
        Assert.Equal(5000m, trade.AvgEntryPrice);
        Assert.Equal(5010m, trade.AvgExitPrice); // salidas: 1 + 3 = 4, todas a 5010
        Assert.Equal(2000m, trade.GrossPnL); // (5010-5000) * 4 * 50
    }

    // ---- Flips ----

    [Fact]
    public void Build_Flip_ClosesOldTradeAndOpensNewOneInOppositeDirection()
    {
        var fills = new[]
        {
            Fill("f1", "ESH6", OrderSide.Buy, 2, 5000m, T(0)),   // long 2
            Fill("f2", "ESH6", OrderSide.Sell, 5, 5010m, T(1)),  // cierra long 2 y abre short 3
            Fill("f3", "ESH6", OrderSide.Buy, 3, 5005m, T(2)),   // cierra short 3
        };

        var trades = TradeBuilder.Build(fills, [Es]);

        Assert.Equal(2, trades.Count);

        var first = trades[0];
        Assert.Equal(TradeDirection.Long, first.Direction);
        Assert.Equal(2, first.Quantity);
        Assert.Equal(5000m, first.AvgEntryPrice);
        Assert.Equal(5010m, first.AvgExitPrice);
        Assert.Equal(1000m, first.GrossPnL); // (5010-5000)*2*50
        Assert.Equal(2, first.Executions.Count); // f1 (apertura) + f2 (cierre/flip)

        var second = trades[1];
        Assert.Equal(TradeDirection.Short, second.Direction);
        Assert.Equal(3, second.Quantity);
        Assert.Equal(5010m, second.AvgEntryPrice); // precio del fill de flip (f2)
        Assert.Equal(5005m, second.AvgExitPrice);
        Assert.Equal(750m, second.GrossPnL); // baja 5 puntos a favor del corto * 3 * 50
        Assert.Single(second.Executions); // solo f3 — f2 (el flip) queda enlazado a la Trade que cierra, no a esta
    }

    // ---- Orden cronológico ----

    [Fact]
    public void Build_OutOfOrderExecutions_ProcessesInChronologicalOrderRegardlessOfInputOrder()
    {
        // El fill de salida aparece PRIMERO en la lista de entrada, pero su ExecutedAt es posterior.
        var fills = new[]
        {
            Fill("f2", "ESH6", OrderSide.Sell, 1, 5010m, T(5)),
            Fill("f1", "ESH6", OrderSide.Buy, 1, 5000m, T(0)),
        };

        var trade = Assert.Single(TradeBuilder.Build(fills, [Es]));

        Assert.Equal(5000m, trade.AvgEntryPrice); // debe reconocer f1 como apertura pese a ir segundo en la lista
        Assert.Equal(5010m, trade.AvgExitPrice);
        Assert.Equal(500m, trade.GrossPnL);
    }

    // ---- Símbolos simultáneos ----

    [Fact]
    public void Build_SimultaneousSymbols_TracksSeparatePositionsPerSymbolEvenWhenInterleaved()
    {
        var fills = new[]
        {
            Fill("es1", "ESH6", OrderSide.Buy, 1, 5000m, T(0)),
            Fill("mes1", "MESH6", OrderSide.Buy, 1, 5000m, T(0)),
            Fill("es2", "ESH6", OrderSide.Sell, 1, 5010m, T(1)),
            Fill("mes2", "MESH6", OrderSide.Sell, 1, 5020m, T(1)),
        };

        var trades = TradeBuilder.Build(fills, [Es, Mes]);

        Assert.Equal(2, trades.Count);

        var esTrade = trades.Single(t => t.Symbol == "ESH6");
        Assert.Equal(500m, esTrade.GrossPnL); // 10 puntos * 50 $/punto

        var mesTrade = trades.Single(t => t.Symbol == "MESH6");
        Assert.Equal(100m, mesTrade.GrossPnL); // 20 puntos * (1.25/0.25) $/punto = 20*5
    }

    // ---- Multiplicadores por instrumento ----

    [Fact]
    public void Build_DifferentInstrumentMultipliers_ScalesPnLByTickValue()
    {
        var esFills = new[]
        {
            Fill("es1", "ESH6", OrderSide.Buy, 1, 5000m, T(0)),
            Fill("es2", "ESH6", OrderSide.Sell, 1, 5004m, T(1)), // +4 puntos
        };
        var mesFills = new[]
        {
            Fill("mes1", "MESH6", OrderSide.Buy, 1, 5000m, T(0)),
            Fill("mes2", "MESH6", OrderSide.Sell, 1, 5004m, T(1)), // mismo movimiento de precio
        };

        var esTrade = Assert.Single(TradeBuilder.Build(esFills, [Es, Mes]));
        var mesTrade = Assert.Single(TradeBuilder.Build(mesFills, [Es, Mes]));

        // MES es 1/10 del tamaño de ES en la vida real (TickValue 1.25 vs 12.50) — mismo movimiento
        // de precio, PnL en proporción 1:10.
        Assert.Equal(esTrade.GrossPnL / 10m, mesTrade.GrossPnL);
    }

    [Fact]
    public void Build_LongerRootWinsOverShorterPrefixMatch()
    {
        // Instrumentos "E" y "ES" ambos serían prefijo de "ESZ5"; debe ganar el más largo ("ES").
        var shortRoot = new Instrument { Id = Guid.NewGuid(), Root = "E", Name = "Genérico E", TickSize = 1m, TickValue = 1m };
        var longRoot = new Instrument { Id = Guid.NewGuid(), Root = "ES", Name = "E-mini S&P 500", TickSize = 0.25m, TickValue = 12.50m };

        var fills = new[]
        {
            Fill("f1", "ESZ5", OrderSide.Buy, 1, 5000m, T(0)),
            Fill("f2", "ESZ5", OrderSide.Sell, 1, 5004m, T(1)),
        };

        var trade = Assert.Single(TradeBuilder.Build(fills, [shortRoot, longRoot]));

        Assert.Equal(200m, trade.GrossPnL); // 4 puntos * (12.50/0.25) = 200, no 4*(1/1)=4
    }

    [Fact]
    public void Build_UnknownSymbol_FallsBackToRawPriceDifference()
    {
        var fills = new[]
        {
            Fill("f1", "GCZ5", OrderSide.Buy, 1, 2000m, T(0)),
            Fill("f2", "GCZ5", OrderSide.Sell, 1, 2010m, T(1)),
        };

        var trade = Assert.Single(TradeBuilder.Build(fills, [Es])); // GC no está en la lista de instrumentos

        Assert.Null(trade.InstrumentId);
        Assert.Equal(10m, trade.GrossPnL); // tickSize=1, tickValue=1 => diferencia de precio en bruto
    }

    // ---- Posiciones abiertas ----

    [Fact]
    public void Build_PositionNeverReturnsToZero_DoesNotProduceAnyClosedTrade()
    {
        var fills = new[]
        {
            Fill("f1", "ESH6", OrderSide.Buy, 1, 5000m, T(0)),
            Fill("f2", "ESH6", OrderSide.Buy, 1, 5010m, T(1)), // sigue abierta, nunca cierra
        };

        var trades = TradeBuilder.Build(fills, [Es]);

        Assert.Empty(trades);
        // La Trade "en curso" descartada no debe dejar Executions apuntando a un Trade.Id
        // fantasma que nunca llega a persistirse (regresión: causaba un DbUpdateConcurrencyException
        // al guardar en un DbContext real, ver ExecutionIngestServiceTests).
        Assert.All(fills, f => Assert.Null(f.TradeId));
    }

    // ---- Comisiones ----

    [Fact]
    public void Build_SumsCommissionsAcrossAllLinkedExecutions()
    {
        var fills = new[]
        {
            Fill("f1", "ESH6", OrderSide.Buy, 1, 5000m, T(0), commission: 2.5m),
            Fill("f2", "ESH6", OrderSide.Sell, 1, 5010m, T(1), commission: 2.5m),
        };

        var trade = Assert.Single(TradeBuilder.Build(fills, [Es]));

        Assert.Equal(5m, trade.Commissions);
    }
}
