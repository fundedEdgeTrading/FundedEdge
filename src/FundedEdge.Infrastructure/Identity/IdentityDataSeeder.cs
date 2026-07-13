using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FundedEdge.Infrastructure.Identity;

/// <summary>
/// Crea los roles de AppRoles si no existen y, si Admin:Email está configurado (User Secrets/
/// entorno) y ese usuario ya está registrado, le asigna el rol Administrator. Idempotente:
/// se ejecuta en cada arranque tras las migraciones.
/// </summary>
public class IdentityDataSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<IdentityDataSeeder> logger)
{
    public async Task SeedAsync()
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = configuration["Admin:Email"];
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            logger.LogWarning("Admin:Email configurado pero no existe ningún usuario con ese email; regístralo y reinicia.");
            return;
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Administrator))
        {
            await userManager.AddToRoleAsync(admin, AppRoles.Administrator);
            logger.LogInformation("Rol Administrator asignado al usuario configurado en Admin:Email.");
        }
    }
}
