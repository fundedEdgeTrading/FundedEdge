using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropFirmRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RulesMarkdown",
                table: "PropFirms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RulesSource",
                table: "PropFirms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RulesSourceUrls",
                table: "PropFirms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RulesUpdatedOn",
                table: "PropFirms",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RulesMarkdown",
                table: "PropFirms");

            migrationBuilder.DropColumn(
                name: "RulesSource",
                table: "PropFirms");

            migrationBuilder.DropColumn(
                name: "RulesSourceUrls",
                table: "PropFirms");

            migrationBuilder.DropColumn(
                name: "RulesUpdatedOn",
                table: "PropFirms");
        }
    }
}
