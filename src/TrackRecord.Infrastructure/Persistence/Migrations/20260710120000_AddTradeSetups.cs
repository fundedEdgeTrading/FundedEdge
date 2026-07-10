using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeSetups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeSetups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeSetups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeSetups_UserId_Name",
                table: "TradeSetups",
                columns: new[] { "UserId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeSetups");
        }
    }
}
