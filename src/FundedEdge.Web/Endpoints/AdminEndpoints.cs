using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FundedEdge.Infrastructure.Identity;

namespace FundedEdge.Web.Endpoints;

/// <summary>
/// Endpoints del panel de administración que necesitan escribir cookies fuera del ciclo de vida
/// de un componente interactivo (mismo motivo que MapAdditionalIdentityEndpoints).
/// </summary>
public static class AdminEndpoints
{
    /// <summary>Claim añadido a la sesión impersonada con el id del administrador que la inició.</summary>
    public const string ImpersonatedByClaim = "ImpersonatedBy";

    public static IEndpointConventionBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/admin")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Administrator));

        // Login automático como otro usuario: cierra la sesión del admin y abre la del usuario
        // objetivo, dejando trazado quién impersona (claim ImpersonatedBy, y log). Para volver a
        // su cuenta, el admin cierra sesión y vuelve a entrar. No se permite impersonar a otros
        // administradores.
        group.MapPost("/impersonate", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] ILoggerFactory loggerFactory,
            [FromForm] string userId) =>
        {
            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var target = await userManager.FindByIdAsync(userId);
            if (target is null)
            {
                return Results.NotFound();
            }

            if (await userManager.IsInRoleAsync(target, AppRoles.Administrator))
            {
                return Results.Forbid();
            }

            loggerFactory.CreateLogger("Admin.Impersonation")
                .LogWarning("El administrador {AdminId} inicia sesión como el usuario {UserId}.", adminId, target.Id);

            await signInManager.SignOutAsync();
            await signInManager.SignInWithClaimsAsync(
                target,
                isPersistent: false,
                [new Claim(ImpersonatedByClaim, adminId)]);

            return Results.LocalRedirect("~/");
        });

        return group;
    }
}
