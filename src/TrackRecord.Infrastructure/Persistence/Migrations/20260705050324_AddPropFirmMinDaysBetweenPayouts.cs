using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropFirmMinDaysBetweenPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinDaysBetweenPayouts",
                table: "PropFirms",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "MinDaysBetweenPayouts",
                value: null);

            migrationBuilder.UpdateData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "MinDaysBetweenPayouts",
                value: null);

            migrationBuilder.UpdateData(
                table: "PropFirms",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "MinDaysBetweenPayouts",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinDaysBetweenPayouts",
                table: "PropFirms");
        }
    }
}
