using FundedEdge.Domain.Enums;

namespace FundedEdge.Application.Abstractions;

/// <summary>
/// Resuelve el plan efectivo de un usuario (incluyendo el trial de Pro) y sus límites. Un
/// usuario en trial vigente se trata como Pro aunque su PlanTier persistido siga siendo Starter.
/// </summary>
public interface IPlanService
{
    /// <summary>Tier efectivo. Sin userId, resuelve el usuario autenticado actual.</summary>
    Task<PlanTier> GetTierAsync(string? userId = null, CancellationToken ct = default);

    Task<PlanLimits> GetLimitsAsync(string? userId = null, CancellationToken ct = default);

    /// <summary>Si el usuario actual puede dar de alta una cuenta de fondeo más (cuenta cuentas no terminales).</summary>
    Task<bool> CanCreateAccountAsync(CancellationToken ct = default);

    /// <summary>Cupo de IA restante del usuario actual (informes completos y preguntas ad-hoc).</summary>
    Task<AiAllowance> GetAiAllowanceAsync(CancellationToken ct = default);
}
