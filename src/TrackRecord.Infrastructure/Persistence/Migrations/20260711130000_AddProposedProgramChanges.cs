using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TrackRecord.Infrastructure.Persistence;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Staging de las propuestas de cambio de catálogo generadas por la extracción LLM (fase 2 de
    /// INVESTIGACION_AUTOMATIZACION_REGLAS.md). La extracción nunca escribe directamente en
    /// EvaluationPrograms: deja aquí la propuesta y el administrador la aprueba o rechaza.
    /// Los atributos [DbContext]/[Migration] se declaran aquí (no en un .Designer.cs aparte)
    /// para que el migrador la descubra en tiempo de ejecución.
    /// </summary>
    [DbContext(typeof(TrackRecordDbContext))]
    [Migration("20260711130000_AddProposedProgramChanges")]
    public partial class AddProposedProgramChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProposedProgramChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropFirmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExistingProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposedProgramChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposedProgramChanges_PropFirms_PropFirmId",
                        column: x => x.PropFirmId,
                        principalTable: "PropFirms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProposedProgramChanges_Status",
                table: "ProposedProgramChanges",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProposedProgramChanges");
        }
    }
}
