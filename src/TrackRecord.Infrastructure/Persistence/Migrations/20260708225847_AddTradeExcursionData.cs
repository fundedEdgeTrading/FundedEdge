using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeExcursionData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxAdverseExcursion",
                table: "Trades",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxFavorableExcursion",
                table: "Trades",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAdverseExcursion",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "MaxFavorableExcursion",
                table: "Trades");
        }
    }
}
