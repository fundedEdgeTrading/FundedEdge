using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Revisión de las propuestas de cambio del catálogo: listar pendientes con su diff,
/// aprobar (aplica el versionado de EvaluationProgram) o rechazar.
/// </summary>
public interface IProgramChangeProposalService
{
    Task<IReadOnlyList<ProposedProgramChangeDto>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>
    /// Aplica la propuesta al catálogo: versiona el programa existente (o lo crea si es nuevo)
    /// mediante IEvaluationProgramService y marca la propuesta como aprobada.
    /// Los campos que la extracción no encontró (null) conservan el valor actual del programa.
    /// </summary>
    Task ApproveAsync(Guid proposalId, CancellationToken ct = default);

    Task RejectAsync(Guid proposalId, CancellationToken ct = default);
}
