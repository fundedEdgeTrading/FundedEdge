using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Instruments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Root = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TickSize = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    TickValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropFirms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropFirms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradingAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropFirmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalAccountId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountSize = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxDrawdown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DrawdownType = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    Feed = table.Column<int>(type: "int", nullable: false),
                    PurchasedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    FundedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ClosedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradingAccounts_PropFirms_PropFirmId",
                        column: x => x.PropFirmId,
                        principalTable: "PropFirms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountCosts_TradingAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStage = table.Column<int>(type: "int", nullable: false),
                    ToStage = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountEvents_TradingAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountRequested = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountReceived = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequestedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_TradingAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Symbol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    AvgEntryPrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    AvgExitPrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GrossPnL = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Commissions = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RiskedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trades_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Trades_TradingAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Side = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Commission = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Executions_Trades_TradeId",
                        column: x => x.TradeId,
                        principalTable: "Trades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Executions_TradingAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Instruments",
                columns: new[] { "Id", "Name", "Root", "TickSize", "TickValue" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000001"), "E-mini S&P 500", "ES", 0.25m, 12.50m },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000002"), "Micro E-mini S&P 500", "MES", 0.25m, 1.25m },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000003"), "E-mini Nasdaq-100", "NQ", 0.25m, 5.00m },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000004"), "Micro E-mini Nasdaq-100", "MNQ", 0.25m, 0.50m },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000005"), "Gold Futures", "GC", 0.10m, 10.00m },
                    { new Guid("aaaaaaaa-0000-0000-0000-000000000006"), "Crude Oil Futures", "CL", 0.01m, 10.00m }
                });

            migrationBuilder.InsertData(
                table: "PropFirms",
                columns: new[] { "Id", "Name", "Notes", "Website" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Lucid Trading", null, "https://lucidtrading.com" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Tradeify", null, "https://tradeify.co" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Apex Trader Funding", null, "https://apextraderfunding.com" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountCosts_AccountId_PaidOn",
                table: "AccountCosts",
                columns: new[] { "AccountId", "PaidOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountEvents_AccountId_OccurredAt",
                table: "AccountEvents",
                columns: new[] { "AccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Executions_AccountId_ExecutedAt",
                table: "Executions",
                columns: new[] { "AccountId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Executions_Source_ExternalId",
                table: "Executions",
                columns: new[] { "Source", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Executions_TradeId",
                table: "Executions",
                column: "TradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Root",
                table: "Instruments",
                column: "Root",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_AccountId_RequestedOn",
                table: "Payouts",
                columns: new[] { "AccountId", "RequestedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_PropFirms_Name",
                table: "PropFirms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trades_AccountId_ClosedAt",
                table: "Trades",
                columns: new[] { "AccountId", "ClosedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_InstrumentId",
                table: "Trades",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingAccounts_PropFirmId",
                table: "TradingAccounts",
                column: "PropFirmId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingAccounts_Stage",
                table: "TradingAccounts",
                column: "Stage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountCosts");

            migrationBuilder.DropTable(
                name: "AccountEvents");

            migrationBuilder.DropTable(
                name: "Executions");

            migrationBuilder.DropTable(
                name: "Payouts");

            migrationBuilder.DropTable(
                name: "Trades");

            migrationBuilder.DropTable(
                name: "Instruments");

            migrationBuilder.DropTable(
                name: "TradingAccounts");

            migrationBuilder.DropTable(
                name: "PropFirms");
        }
    }
}
