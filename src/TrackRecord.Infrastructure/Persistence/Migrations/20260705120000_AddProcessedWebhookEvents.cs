using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TrackRecord.Infrastructure.Persistence;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Tabla de idempotencia para los webhooks de facturación ya procesados (SEC-16). Los atributos
    /// [DbContext]/[Migration] se declaran aquí (no en un .Designer.cs aparte) para que el migrador
    /// la descubra y la aplique en tiempo de ejecución. El snapshot del modelo sí se actualiza para
    /// que un futuro `dotnet ef migrations add` produzca un diff limpio.
    /// </summary>
    [DbContext(typeof(TrackRecordDbContext))]
    [Migration("20260705120000_AddProcessedWebhookEvents")]
    public partial class AddProcessedWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedWebhookEvents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedWebhookEvents");
        }
    }
}
