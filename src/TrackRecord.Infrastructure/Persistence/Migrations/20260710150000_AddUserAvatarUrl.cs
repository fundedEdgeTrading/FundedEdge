using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TrackRecord.Infrastructure.Persistence;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Columna AvatarUrl en AspNetUsers. Los atributos [DbContext]/[Migration] se declaran aquí
    /// (no en un .Designer.cs aparte) para que el migrador la descubra y la aplique en tiempo de
    /// ejecución. El snapshot del modelo sí se actualiza para que un futuro
    /// `dotnet ef migrations add` produzca un diff limpio.
    /// </summary>
    [DbContext(typeof(TrackRecordDbContext))]
    [Migration("20260710150000_AddUserAvatarUrl")]
    public partial class AddUserAvatarUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "AspNetUsers");
        }
    }
}
