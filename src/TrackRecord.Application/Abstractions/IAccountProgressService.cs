using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Calcula el progreso de una cuenta de trading contra las reglas del programa de evaluación
/// vinculado. Devuelve un DTO diferente según la etapa actual de la cuenta (Evaluation / Funded).
/// </summary>
public interface IAccountProgressService
{
    /// <summary>
    /// Devuelve el progreso de la cuenta indicada. Retorna null si la cuenta no existe,
    /// no pertenece al usuario actual o no tiene un <c>EvaluationProgramId</c> vinculado.
    /// </summary>
    Task<AccountProgressDto?> GetProgressAsync(Guid accountId, CancellationToken ct = default);
}
