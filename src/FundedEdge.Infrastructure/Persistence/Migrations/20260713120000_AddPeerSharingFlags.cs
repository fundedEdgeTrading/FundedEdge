using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPeerSharingFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShareOperativa",
                table: "PublicProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShareEmotions",
                table: "PublicProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShareOperativa",
                table: "PublicProfiles");

            migrationBuilder.DropColumn(
                name: "ShareEmotions",
                table: "PublicProfiles");
        }
    }
}
