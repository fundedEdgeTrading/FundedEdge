using FundedEdge.Application.Dtos;

namespace FundedEdge.Application.Abstractions;

/// <summary>
/// Orquesta la sincronización automatizada de reglas de una firma: descarga sus fuentes con Nimble,
/// extrae los programas con IA y actualiza el reglamento editorial de la firma, devolviendo los
/// programas propuestos para que el administrador los revise antes de guardarlos. Coordina
/// <see cref="INimbleClient"/>, <see cref="IRuleExtractionService"/> y
/// <see cref="IExternalFirmDataProvider"/> sin que ninguno conozca a los demás (bajo acoplamiento).
/// </summary>
public interface IFirmRulesSyncService
{
    /// <summary>True si Nimble y la IA están ambos configurados.</summary>
    bool IsConfigured { get; }

    /// <summary>Sincroniza las reglas de la firma <paramref name="firmId"/> desde sus URLs fuente.</summary>
    Task<FirmRulesSyncResult> SyncAsync(Guid firmId, CancellationToken ct = default);
}
