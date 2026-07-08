using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationProgramFundedAndPayoutRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EvaluationProgramId",
                table: "TradingAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FundedDailyLossLimit",
                table: "EvaluationPrograms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FundedDrawdownType",
                table: "EvaluationPrograms",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FundedMaxDrawdown",
                table: "EvaluationPrograms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FundedMinTradingDays",
                table: "EvaluationPrograms",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FundedProfitTarget",
                table: "EvaluationPrograms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PayoutMaxProfitPct",
                table: "EvaluationPrograms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayoutMinDaysBetween",
                table: "EvaluationPrograms",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PayoutSplitTraderPct",
                table: "EvaluationPrograms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "PropFirmId1",
                table: "EvaluationPrograms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000001"),
                columns: new[] { "FundedDailyLossLimit", "FundedDrawdownType", "FundedMaxDrawdown", "FundedMinTradingDays", "FundedProfitTarget", "PayoutMaxProfitPct", "PayoutMinDaysBetween", "PayoutSplitTraderPct", "PropFirmId1" },
                values: new object[] { null, 0, 2500m, 7, null, null, 7, 1.00m, null });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000002"),
                columns: new[] { "FundedDailyLossLimit", "FundedDrawdownType", "FundedMaxDrawdown", "FundedMinTradingDays", "FundedProfitTarget", "PayoutMaxProfitPct", "PayoutMinDaysBetween", "PayoutSplitTraderPct", "PropFirmId1" },
                values: new object[] { null, 0, 3000m, 7, null, null, 7, 1.00m, null });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000003"),
                columns: new[] { "FundedDailyLossLimit", "FundedDrawdownType", "FundedMaxDrawdown", "FundedMinTradingDays", "FundedProfitTarget", "PayoutMaxProfitPct", "PayoutMinDaysBetween", "PayoutSplitTraderPct", "PropFirmId1" },
                values: new object[] { null, 1, 2000m, null, null, null, 14, 0.80m, null });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000004"),
                columns: new[] { "FundedDailyLossLimit", "FundedDrawdownType", "FundedMaxDrawdown", "FundedMinTradingDays", "FundedProfitTarget", "PayoutMaxProfitPct", "PayoutMinDaysBetween", "PayoutSplitTraderPct", "PropFirmId1" },
                values: new object[] { null, 1, 3000m, null, null, null, 14, 0.80m, null });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000005"),
                columns: new[] { "FundedDailyLossLimit", "FundedDrawdownType", "FundedMaxDrawdown", "FundedMinTradingDays", "FundedProfitTarget", "PayoutMaxProfitPct", "PayoutMinDaysBetween", "PayoutSplitTraderPct", "PropFirmId1" },
                values: new object[] { 1250m, 2, 2000m, null, null, 0.50m, 14, 0.90m, null });

            migrationBuilder.UpdateData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000006"),
                columns: new[] { "FundedDailyLossLimit", "FundedDrawdownType", "FundedMaxDrawdown", "FundedMinTradingDays", "FundedProfitTarget", "PayoutMaxProfitPct", "PayoutMinDaysBetween", "PayoutSplitTraderPct", "PropFirmId1" },
                values: new object[] { 2500m, 2, 3000m, null, null, 0.50m, 14, 0.90m, null });

            migrationBuilder.InsertData(
                table: "PropFirms",
                columns: new[] { "Id", "MinDaysBetweenPayouts", "Name", "Notes", "Website" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), null, "Topstep", null, "https://topstep.com" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), null, "MyFundedFutures", null, "https://myfundedfutures.com" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), null, "Take Profit Trader", null, "https://takeprofittrader.com" },
                    { new Guid("77777777-7777-7777-7777-777777777777"), null, "Earn2Trade", null, "https://earn2trade.com" }
                });

            migrationBuilder.InsertData(
                table: "EvaluationPrograms",
                columns: new[] { "Id", "AccountSize", "ActivationCost", "ConsistencyMaxDayFraction", "DailyLossLimit", "DrawdownType", "EffectiveFrom", "EvaluationCost", "FundedDailyLossLimit", "FundedDrawdownType", "FundedMaxDrawdown", "FundedMinTradingDays", "FundedProfitTarget", "IsActive", "MaxDrawdown", "MinTradingDays", "Name", "PayoutMaxProfitPct", "PayoutMinDaysBetween", "PayoutSplitTraderPct", "ProfitTarget", "PropFirmId", "PropFirmId1" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-0000-0000-000000000007"), 50000m, 149m, null, 1000m, 0, new DateOnly(2026, 1, 1), 165m, 1000m, 0, 2000m, null, null, true, 2000m, 5, "Topstep 50K", null, 7, 0.90m, 3000m, new Guid("44444444-4444-4444-4444-444444444444"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000008"), 100000m, 149m, null, 2000m, 0, new DateOnly(2026, 1, 1), 245m, 2000m, 0, 3000m, null, null, true, 3000m, 5, "Topstep 100K", null, 7, 0.90m, 6000m, new Guid("44444444-4444-4444-4444-444444444444"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000009"), 50000m, 135m, null, 1000m, 0, new DateOnly(2026, 1, 1), 165m, 1000m, 0, 2500m, null, null, true, 2500m, null, "MFF 50K", null, 14, 0.90m, 3000m, new Guid("55555555-5555-5555-5555-555555555555"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000010"), 100000m, 135m, null, 2000m, 0, new DateOnly(2026, 1, 1), 250m, 2000m, 0, 3000m, null, null, true, 3000m, null, "MFF 100K", null, 14, 0.90m, 6000m, new Guid("55555555-5555-5555-5555-555555555555"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000011"), 50000m, 130m, null, null, 0, new DateOnly(2026, 1, 1), 150m, null, 0, 2500m, null, null, true, 2500m, 10, "TPT 50K", null, 14, 0.85m, 3000m, new Guid("66666666-6666-6666-6666-666666666666"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000012"), 100000m, 130m, null, null, 0, new DateOnly(2026, 1, 1), 220m, null, 0, 3000m, null, null, true, 3000m, 10, "TPT 100K", null, 14, 0.85m, 6000m, new Guid("66666666-6666-6666-6666-666666666666"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000013"), 25000m, 0m, null, null, 0, new DateOnly(2026, 1, 1), 150m, null, 0, 1500m, null, null, true, 1500m, 15, "E2T Gauntlet Mini 25K", null, 30, 0.80m, 1500m, new Guid("77777777-7777-7777-7777-777777777777"), null },
                    { new Guid("b0000000-0000-0000-0000-000000000014"), 50000m, 0m, null, null, 0, new DateOnly(2026, 1, 1), 245m, null, 0, 2000m, null, null, true, 2000m, 15, "E2T Gauntlet 50K", null, 30, 0.80m, 3000m, new Guid("77777777-7777-7777-7777-777777777777"), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradingAccounts_EvaluationProgramId",
                table: "TradingAccounts",
                column: "EvaluationProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPrograms_PropFirmId1",
                table: "EvaluationPrograms",
                column: "PropFirmId1");

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationPrograms_PropFirms_PropFirmId1",
                table: "EvaluationPrograms",
                column: "PropFirmId1",
                principalTable: "PropFirms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TradingAccounts_EvaluationPrograms_EvaluationProgramId",
                table: "TradingAccounts",
                column: "EvaluationProgramId",
                principalTable: "EvaluationPrograms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationPrograms_PropFirms_PropFirmId1",
                table: "EvaluationPrograms");

            migrationBuilder.DropForeignKey(
                name: "FK_TradingAccounts_EvaluationPrograms_EvaluationProgramId",
                table: "TradingAccounts");

            migrationBuilder.DropIndex(
                name: "IX_TradingAccounts_EvaluationProgramId",
                table: "TradingAccounts");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationPrograms_PropFirmId1",
                table: "EvaluationPrograms");

            migrationBuilder.DeleteData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "EvaluationPrograms",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DeleteData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DropColumn(
                name: "EvaluationProgramId",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "FundedDailyLossLimit",
                table: "EvaluationPrograms");

            migrationBuilder.DropColumn(
                name: "FundedDrawdownType",
                table: "EvaluationPrograms");

            migrationBuilder.DropColumn(
                name: "FundedMaxDrawdown",
                table: "EvaluationPrograms");

            migrationBuilder.DropColumn(
                name: "FundedMinTradingDays",
                table: "EvaluationPrograms");

            migrationBuilder.DropColumn(
                name: "FundedProfitTarget",
                table: "EvaluationPrograms");

            migrationBuilder.DropColumn(
                name: "PayoutMaxProfitPct",
                table: "EvaluationPrograms");

            migrationBuilder.DropColumn(
                name: "PayoutMinDaysBetween",
                table: "EvaluationPrograms");

            migrationBuilder.DropColumn(
                name: "PayoutSplitTraderPct",
                table: "EvaluationPrograms");

            migrationBuilder.DropColumn(
                name: "PropFirmId1",
                table: "EvaluationPrograms");
        }
    }
}
