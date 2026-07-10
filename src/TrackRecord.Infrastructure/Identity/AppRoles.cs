namespace TrackRecord.Infrastructure.Identity;

/// <summary>
/// Roles de la plataforma. Los roles son PERMISOS (qué puedes hacer), no planes: el plan
/// comprado (Starter/Pro/Elite) ya vive en ApplicationUser.PlanTier y limita features vía
/// IPlanService — no necesita roles. Aquí solo hay jerarquía operativa.
/// </summary>
public static class AppRoles
{
    /// <summary>Acceso total al panel /admin: eliminar usuarios, impersonar, KPIs globales.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Soporte al cliente: acceso de solo lectura al panel /admin (listado y KPIs), sin eliminar ni impersonar.</summary>
    public const string Support = "Support";

    public static readonly string[] All = [Administrator, Support];
}
