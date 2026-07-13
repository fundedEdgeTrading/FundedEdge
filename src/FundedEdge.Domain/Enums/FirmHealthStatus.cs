namespace FundedEdge.Domain.Enums;

/// <summary>
/// Estado editorial de salud de una prop firm (PLAN_IMPLEMENTACION_MERCADO.md M6): tras la purga
/// de firmas 2024–2026, la confianza es criterio de compra. Lo mantiene el administrador del
/// catálogo compartido; un cambio de estado avisa a los usuarios con cuentas activas en la firma.
/// </summary>
public enum FirmHealthStatus
{
    /// <summary>Operativa con normalidad, sin señales públicas de riesgo.</summary>
    Active = 0,

    /// <summary>En observación: cambios de reglas recientes, retrasos de payout reportados u otras señales.</summary>
    Watch = 1,

    /// <summary>Cerrada o fuera del mercado: no comprar evaluaciones nuevas.</summary>
    Closed = 2,
}
