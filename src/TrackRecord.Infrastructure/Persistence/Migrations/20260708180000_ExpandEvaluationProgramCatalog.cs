using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TrackRecord.Infrastructure.Persistence;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Amplía el catálogo de programas de evaluación (módulo Firm Fit) para cubrir todas las
    /// modalidades de cuenta (tamaños) de las 7 firmas ya sembradas, y corrige reglas de
    /// evaluación/fondeo/payout de los programas existentes con datos más precisos (ver
    /// SeedData.Programs). Solo datos: no cambia el esquema. Self-contained (sin .Designer.cs,
    /// ver comentario en 20260705130000_AddEvaluationPrograms) con columnTypes explícitos.
    /// </summary>
    [DbContext(typeof(TrackRecordDbContext))]
    [Migration("20260708180000_ExpandEvaluationProgramCatalog")]
    public partial class ExpandEvaluationProgramCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Tradeify 50K/100K: añade daily loss limit (Growth, soft breach) y corrige split a 90/10 ──
            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000003"),
                columns: new[] { "DailyLossLimit", "FundedDailyLossLimit", "PayoutSplitTraderPct" },
                values: new object[] { 1250m, 1250m, 0.90m });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000004"),
                columns: new[] { "DailyLossLimit", "FundedDailyLossLimit", "PayoutSplitTraderPct" },
                values: new object[] { 2500m, 2500m, 0.90m });

            // ── Topstep 50K/100K: payouts quincenales (14 días), no semanales ──────────────────
            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000007"),
                columns: new[] { "PayoutMinDaysBetween" }, values: new object[] { 14 });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000008"),
                columns: new[] { "PayoutMinDaysBetween" }, values: new object[] { 14 });

            // ── MyFundedFutures 50K/100K: recaracteriza como plan Rapid (sin activación, sin daily
            //    loss, consistencia 50%, payouts cada 5 días) ────────────────────────────────────
            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000009"),
                columns: new[] { "Name", "ActivationCost", "MaxDrawdown", "DailyLossLimit", "ConsistencyMaxDayFraction", "FundedMaxDrawdown", "FundedDailyLossLimit", "PayoutMinDaysBetween" },
                values: new object[] { "MFF Rapid 50K", 0m, 2000m, null, 0.50m, 2000m, null, 5 });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000010"),
                columns: new[] { "Name", "ActivationCost", "DailyLossLimit", "ConsistencyMaxDayFraction", "FundedDailyLossLimit", "PayoutMinDaysBetween" },
                values: new object[] { "MFF Rapid 100K", 0m, null, 0.50m, null, 5 });

            // ── Take Profit Trader 50K/100K: EOD en evaluación (pasa a intradía al fondear), split
            //    80/20, mínimo 5 días (no 10) ─────────────────────────────────────────────────────
            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000011"),
                columns: new[] { "MaxDrawdown", "DrawdownType", "MinTradingDays", "FundedMaxDrawdown", "PayoutSplitTraderPct" },
                values: new object[] { 2000m, 1, 5, 2000m, 0.80m });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000012"),
                columns: new[] { "DrawdownType", "MinTradingDays", "PayoutSplitTraderPct" },
                values: new object[] { 1, 5, 0.80m });

            // ── Earn2Trade: renombra/reescala los dos programas existentes a la gama real de
            //    Gauntlet Mini (arranca en 50K, no 25K) y corrige tipo de drawdown a EOD ──────────
            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000013"),
                columns: new[] { "Name", "AccountSize", "EvaluationCost", "ProfitTarget", "MaxDrawdown", "DrawdownType", "DailyLossLimit", "MinTradingDays", "ConsistencyMaxDayFraction", "FundedMaxDrawdown", "FundedDrawdownType" },
                values: new object[] { "E2T Gauntlet Mini 100K", 100000m, 430m, 6000m, 4000m, 1, 2200m, 10, 0.30m, 4000m, 1 });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000014"),
                columns: new[] { "Name", "DrawdownType", "DailyLossLimit", "MinTradingDays", "ConsistencyMaxDayFraction", "FundedDrawdownType" },
                values: new object[] { "E2T Gauntlet Mini 50K", 1, 1100m, 10, 0.30m, 1 });

            // ── Nuevas modalidades de cuenta (todos los tamaños que faltaban) ──────────────────
            migrationBuilder.InsertData(
                table: "EvaluationPrograms",
                columns: new[] { "Id", "AccountSize", "ActivationCost", "ConsistencyMaxDayFraction", "DailyLossLimit", "DrawdownType", "EffectiveFrom", "EvaluationCost", "FundedDailyLossLimit", "FundedDrawdownType", "FundedMaxDrawdown", "FundedMinTradingDays", "FundedProfitTarget", "IsActive", "MaxDrawdown", "MinTradingDays", "Name", "PayoutMaxProfitPct", "PayoutMinDaysBetween", "PayoutSplitTraderPct", "ProfitTarget", "PropFirmId", "PropFirmId1" },
                columnTypes: new[] { "uniqueidentifier", "decimal(18,2)", "decimal(18,2)", "decimal(5,4)", "decimal(18,2)", "int", "date", "decimal(18,2)", "decimal(18,2)", "int", "decimal(18,2)", "int", "decimal(18,2)", "bit", "decimal(18,2)", "int", "nvarchar(200)", "decimal(18,2)", "int", "decimal(18,2)", "decimal(18,2)", "uniqueidentifier", "uniqueidentifier" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-0000-0000-000000000015"), 25000m, 130m, 0.30m, null, 0, new DateOnly(2026, 1, 1), 147m, null, 0, 1500m, 7, null, true, 1500m, 7, "Apex 25K", null, 7, 1.00m, 1500m, new Guid("33333333-3333-3333-3333-333333333333"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000016"), 150000m, 130m, 0.30m, null, 0, new DateOnly(2026, 1, 1), 297m, null, 0, 5000m, 7, null, true, 5000m, 7, "Apex 150K", null, 7, 1.00m, 9000m, new Guid("33333333-3333-3333-3333-333333333333"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000017"), 25000m, 0m, null, 600m, 1, new DateOnly(2026, 1, 1), 130m, 600m, 1, 1000m, null, null, true, 1000m, 5, "Tradeify Growth 25K", null, 14, 0.90m, 1500m, new Guid("22222222-2222-2222-2222-222222222222"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000018"), 150000m, 0m, null, 3750m, 1, new DateOnly(2026, 1, 1), 275m, 3750m, 1, 5000m, null, null, true, 5000m, 5, "Tradeify Growth 150K", null, 14, 0.90m, 9000m, new Guid("22222222-2222-2222-2222-222222222222"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000019"), 25000m, 0m, null, 625m, 2, new DateOnly(2026, 1, 1), 95m, 625m, 2, 1000m, null, null, true, 1000m, null, "Lucid 25K", 0.50m, 14, 0.90m, 1000m, new Guid("11111111-1111-1111-1111-111111111111"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000020"), 150000m, 0m, null, 3750m, 2, new DateOnly(2026, 1, 1), 365m, 3750m, 2, 4500m, null, null, true, 4500m, null, "Lucid 150K", 0.50m, 14, 0.90m, 6000m, new Guid("11111111-1111-1111-1111-111111111111"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000021"), 150000m, 149m, null, 3000m, 0, new DateOnly(2026, 1, 1), 199m, 3000m, 0, 4500m, null, null, true, 4500m, 5, "Topstep 150K", null, 14, 0.90m, 9000m, new Guid("44444444-4444-4444-4444-444444444444"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000022"), 25000m, 0m, 0.50m, null, 0, new DateOnly(2026, 1, 1), 120m, null, 0, 1000m, null, null, true, 1000m, null, "MFF Rapid 25K", null, 5, 0.90m, 1500m, new Guid("55555555-5555-5555-5555-555555555555"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000023"), 150000m, 0m, 0.50m, null, 0, new DateOnly(2026, 1, 1), 320m, null, 0, 4500m, null, null, true, 4500m, null, "MFF Rapid 150K", null, 5, 0.90m, 9000m, new Guid("55555555-5555-5555-5555-555555555555"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000024"), 25000m, 130m, null, null, 1, new DateOnly(2026, 1, 1), 150m, null, 0, 1500m, null, null, true, 1500m, 5, "TPT 25K", null, 14, 0.80m, 1500m, new Guid("66666666-6666-6666-6666-666666666666"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000025"), 75000m, 130m, null, null, 1, new DateOnly(2026, 1, 1), 185m, null, 0, 3000m, null, null, true, 3000m, 5, "TPT 75K", null, 14, 0.80m, 4500m, new Guid("66666666-6666-6666-6666-666666666666"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000026"), 150000m, 130m, null, null, 1, new DateOnly(2026, 1, 1), 300m, null, 0, 4500m, null, null, true, 4500m, 5, "TPT 150K", null, 14, 0.80m, 9000m, new Guid("66666666-6666-6666-6666-666666666666"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000027"), 150000m, 0m, 0.30m, 3300m, 1, new DateOnly(2026, 1, 1), 600m, null, 1, 6000m, null, null, true, 6000m, 10, "E2T Gauntlet Mini 150K", null, 30, 0.80m, 9000m, new Guid("77777777-7777-7777-7777-777777777777"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000028"), 200000m, 0m, 0.30m, 4400m, 1, new DateOnly(2026, 1, 1), 750m, null, 1, 8000m, null, null, true, 8000m, 10, "E2T Gauntlet Mini 200K", null, 30, 0.80m, 12000m, new Guid("77777777-7777-7777-7777-777777777777"), null },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000015"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000016"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000017"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000018"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000019"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000020"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000021"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000022"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000023"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000024"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000025"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000026"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000027"));
            migrationBuilder.DeleteData(table: "EvaluationPrograms", keyColumn: "Id", keyValue: new Guid("b0000000-0000-0000-0000-000000000028"));

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000003"),
                columns: new[] { "DailyLossLimit", "FundedDailyLossLimit", "PayoutSplitTraderPct" },
                values: new object[] { null, null, 0.80m });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000004"),
                columns: new[] { "DailyLossLimit", "FundedDailyLossLimit", "PayoutSplitTraderPct" },
                values: new object[] { null, null, 0.80m });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000007"),
                columns: new[] { "PayoutMinDaysBetween" }, values: new object[] { 7 });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000008"),
                columns: new[] { "PayoutMinDaysBetween" }, values: new object[] { 7 });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000009"),
                columns: new[] { "Name", "ActivationCost", "MaxDrawdown", "DailyLossLimit", "ConsistencyMaxDayFraction", "FundedMaxDrawdown", "FundedDailyLossLimit", "PayoutMinDaysBetween" },
                values: new object[] { "MFF 50K", 135m, 2500m, 1000m, null, 2500m, 1000m, 14 });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000010"),
                columns: new[] { "Name", "ActivationCost", "DailyLossLimit", "ConsistencyMaxDayFraction", "FundedDailyLossLimit", "PayoutMinDaysBetween" },
                values: new object[] { "MFF 100K", 135m, 2000m, null, 2000m, 14 });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000011"),
                columns: new[] { "MaxDrawdown", "DrawdownType", "MinTradingDays", "FundedMaxDrawdown", "PayoutSplitTraderPct" },
                values: new object[] { 2500m, 0, 10, 2500m, 0.85m });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000012"),
                columns: new[] { "DrawdownType", "MinTradingDays", "PayoutSplitTraderPct" },
                values: new object[] { 0, 10, 0.85m });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000013"),
                columns: new[] { "Name", "AccountSize", "EvaluationCost", "ProfitTarget", "MaxDrawdown", "DrawdownType", "DailyLossLimit", "MinTradingDays", "ConsistencyMaxDayFraction", "FundedMaxDrawdown", "FundedDrawdownType" },
                values: new object[] { "E2T Gauntlet Mini 25K", 25000m, 150m, 1500m, 1500m, 0, null, 15, null, 1500m, 0 });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms", keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000014"),
                columns: new[] { "Name", "DrawdownType", "DailyLossLimit", "MinTradingDays", "ConsistencyMaxDayFraction", "FundedDrawdownType" },
                values: new object[] { "E2T Gauntlet 50K", 0, null, 15, null, 0 });
        }
    }
}
