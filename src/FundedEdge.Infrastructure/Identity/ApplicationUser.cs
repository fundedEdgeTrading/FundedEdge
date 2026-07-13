using Microsoft.AspNetCore.Identity;
using FundedEdge.Domain.Enums;

namespace FundedEdge.Infrastructure.Identity;

/// <summary>
/// Usuario de la plataforma. Cada uno ve únicamente sus propias cuentas de fondeo, trades,
/// costes, payouts, informes de IA e integraciones — las firmas de fondeo y el catálogo de
/// instrumentos son compartidos entre todos los usuarios (ver README).
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    /// <summary>Ruta relativa del avatar: uno de los predefinidos de /img/avatars o una foto subida a /uploads/avatars. Null = sin avatar (se muestra la inicial).</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Plan de suscripción persistido. Ver IPlanService para el tier EFECTIVO (tiene en cuenta el trial).</summary>
    public PlanTier PlanTier { get; set; } = PlanTier.Starter;

    /// <summary>Fin del trial de Pro (14 días desde el registro). Null = sin trial (nunca lo tuvo o ya expiró y se limpió).</summary>
    public DateTimeOffset? TrialEndsAt { get; set; }

    /// <summary>Id de cliente en Stripe. Null hasta el primer checkout completado.</summary>
    public string? StripeCustomerId { get; set; }
}
