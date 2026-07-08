using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackRecord.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migración de datos: al activar RequireConfirmedAccount, los usuarios creados antes de que
    /// existiera la verificación de email quedarían bloqueados (EmailConfirmed = 0). Se les da por
    /// confirmados — la verificación aplica solo a los registros nuevos desde este despliegue.
    /// </summary>
    public partial class ConfirmExistingUserEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE EmailConfirmed = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible a propósito: no sabemos qué usuarios estaban sin confirmar antes.
        }
    }
}
