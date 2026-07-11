using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TrackRecord.Infrastructure.Persistence;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Fuentes monitorizadas de reglas (fase 1 de INVESTIGACION_AUTOMATIZACION_REGLAS.md):
    /// URLs oficiales de cada firma cuyo contenido se hashea a diario para detectar cambios de
    /// condiciones y avisar al administrador. Los atributos [DbContext]/[Migration] se declaran
    /// aquí (no en un .Designer.cs aparte) para que el migrador la descubra en tiempo de ejecución.
    /// </summary>
    [DbContext(typeof(TrackRecordDbContext))]
    [Migration("20260711120000_AddRuleSources")]
    public partial class AddRuleSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RuleSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropFirmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleSources_PropFirms_PropFirmId",
                        column: x => x.PropFirmId,
                        principalTable: "PropFirms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuleSources_PropFirmId_IsEnabled",
                table: "RuleSources",
                columns: new[] { "PropFirmId", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuleSources");
        }
    }
}
