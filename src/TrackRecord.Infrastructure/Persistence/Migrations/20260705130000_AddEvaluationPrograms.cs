using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TrackRecord.Infrastructure.Persistence;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Catálogo de programas de evaluación (módulo Firm Fit): cada firma ofrece programas con sus
    /// reglas exactas (coste, target, drawdown, pérdida diaria, días mínimos, consistencia) contra
    /// las que se simula la operativa real del usuario para recomendar qué comprar. Los atributos
    /// [DbContext]/[Migration] se declaran aquí (no en un .Designer.cs aparte) para que el migrador
    /// la descubra y la aplique en tiempo de ejecución. Siembra el catálogo inicial (SeedData.Programs).
    /// </summary>
    [DbContext(typeof(TrackRecordDbContext))]
    [Migration("20260705130000_AddEvaluationPrograms")]
    public partial class AddEvaluationPrograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropFirmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountSize = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EvaluationCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActivationCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxDrawdown = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DrawdownType = table.Column<int>(type: "int", nullable: false),
                    DailyLossLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinTradingDays = table.Column<int>(type: "int", nullable: true),
                    ConsistencyMaxDayFraction = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationPrograms_PropFirms_PropFirmId",
                        column: x => x.PropFirmId,
                        principalTable: "PropFirms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // columnTypes explícitos: al ser una migración self-contained (sin .Designer.cs con
            // BuildTargetModel), el generador de SQL no puede resolver los tipos de columna del
            // InsertData desde el modelo objetivo de la migración y lanzaría "There is no entity type
            // mapped to the table 'EvaluationPrograms'". Dárselos aquí evita esa dependencia.
            migrationBuilder.InsertData(
                table: "EvaluationPrograms",
                columns: new[] { "Id", "PropFirmId", "Name", "AccountSize", "EvaluationCost", "ActivationCost", "ProfitTarget", "MaxDrawdown", "DrawdownType", "DailyLossLimit", "MinTradingDays", "ConsistencyMaxDayFraction", "EffectiveFrom", "IsActive" },
                columnTypes: new[] { "uniqueidentifier", "uniqueidentifier", "nvarchar(200)", "decimal(18,2)", "decimal(18,2)", "decimal(18,2)", "decimal(18,2)", "decimal(18,2)", "int", "decimal(18,2)", "int", "decimal(5,4)", "date", "bit" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-0000-0000-000000000001"), new Guid("33333333-3333-3333-3333-333333333333"), "Apex 50K", 50000m, 167m, 130m, 3000m, 2500m, 0, null, 7, 0.30m, new DateOnly(2026, 1, 1), true },
                    { new Guid("b0000000-0000-0000-0000-000000000002"), new Guid("33333333-3333-3333-3333-333333333333"), "Apex 100K", 100000m, 207m, 130m, 6000m, 3000m, 0, null, 7, 0.30m, new DateOnly(2026, 1, 1), true },
                    { new Guid("b0000000-0000-0000-0000-000000000003"), new Guid("22222222-2222-2222-2222-222222222222"), "Tradeify Growth 50K", 50000m, 165m, 0m, 3000m, 2000m, 1, null, 5, null, new DateOnly(2026, 1, 1), true },
                    { new Guid("b0000000-0000-0000-0000-000000000004"), new Guid("22222222-2222-2222-2222-222222222222"), "Tradeify Advanced 100K", 100000m, 219m, 0m, 6000m, 3000m, 1, null, 5, null, new DateOnly(2026, 1, 1), true },
                    { new Guid("b0000000-0000-0000-0000-000000000005"), new Guid("11111111-1111-1111-1111-111111111111"), "Lucid 50K", 50000m, 137m, 0m, 2000m, 2000m, 2, 1250m, null, null, new DateOnly(2026, 1, 1), true },
                    { new Guid("b0000000-0000-0000-0000-000000000006"), new Guid("11111111-1111-1111-1111-111111111111"), "Lucid 100K", 100000m, 267m, 0m, 4000m, 3000m, 2, 2500m, null, null, new DateOnly(2026, 1, 1), true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPrograms_PropFirmId_IsActive",
                table: "EvaluationPrograms",
                columns: new[] { "PropFirmId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationPrograms");
        }
    }
}
