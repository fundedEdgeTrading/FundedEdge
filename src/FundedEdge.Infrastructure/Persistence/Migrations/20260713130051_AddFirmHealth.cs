using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFirmHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "PropFirms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthNotes",
                table: "PropFirms",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HealthStatus",
                table: "PropFirms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "HealthUpdatedOn",
                table: "PropFirms",
                type: "date",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "Country", "HealthNotes", "HealthStatus", "HealthUpdatedOn" },
                values: new object[] { null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "Country", "HealthNotes", "HealthStatus", "HealthUpdatedOn" },
                values: new object[] { null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "Country", "HealthNotes", "HealthStatus", "HealthUpdatedOn" },
                values: new object[] { null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "Country", "HealthNotes", "HealthStatus", "HealthUpdatedOn" },
                values: new object[] { null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "Country", "HealthNotes", "HealthStatus", "HealthUpdatedOn" },
                values: new object[] { null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "Country", "HealthNotes", "HealthStatus", "HealthUpdatedOn" },
                values: new object[] { null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "Country", "HealthNotes", "HealthStatus", "HealthUpdatedOn" },
                values: new object[] { null, null, 0, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "PropFirms");

            migrationBuilder.DropColumn(
                name: "HealthNotes",
                table: "PropFirms");

            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "PropFirms");

            migrationBuilder.DropColumn(
                name: "HealthUpdatedOn",
                table: "PropFirms");
        }
    }
}
