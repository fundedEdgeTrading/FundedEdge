using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TrackRecord.Infrastructure.Persistence;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Módulo de psicología del trading (GUIA_PSICOLOGIA_TRADING.md §9): diario emocional por
    /// trade y check-in diario. Los atributos [DbContext]/[Migration] se declaran aquí (no en un
    /// .Designer.cs aparte) para que el migrador la descubra y la aplique en tiempo de ejecución,
    /// siguiendo el mismo patrón que AddProcessedWebhookEvents. El snapshot del modelo se
    /// actualiza para que un futuro `dotnet ef migrations add` produzca un diff limpio.
    /// </summary>
    [DbContext(typeof(TrackRecordDbContext))]
    [Migration("20260708190000_AddPsychologyModule")]
    public partial class AddPsychologyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeEmotionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Moment = table.Column<int>(type: "int", nullable: false),
                    Emotion = table.Column<int>(type: "int", nullable: false),
                    Intensity = table.Column<int>(type: "int", nullable: false),
                    Adherence = table.Column<int>(type: "int", nullable: false),
                    WasImpulsive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LoggedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeEmotionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeEmotionLogs_Trades_TradeId",
                        column: x => x.TradeId,
                        principalTable: "Trades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyMindsetCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    SleepQuality = table.Column<int>(type: "int", nullable: false),
                    ExternalStress = table.Column<int>(type: "int", nullable: false),
                    PreMarketFocus = table.Column<int>(type: "int", nullable: false),
                    DominantPreMarketEmotion = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMindsetCheckIns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeEmotionLogs_TradeId",
                table: "TradeEmotionLogs",
                column: "TradeId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMindsetCheckIns_UserId_Date",
                table: "DailyMindsetCheckIns",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeEmotionLogs");

            migrationBuilder.DropTable(
                name: "DailyMindsetCheckIns");
        }
    }
}
