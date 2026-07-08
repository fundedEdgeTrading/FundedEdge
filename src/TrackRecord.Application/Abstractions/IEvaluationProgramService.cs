using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// Gestión del catálogo de programas de evaluación por firma.
/// Los programas son datos compartidos (sin UserId); cualquier usuario autenticado puede leerlos.
/// La escritura está destinada a administradores o al propio usuario que gestiona su catálogo.
/// </summary>
public interface IEvaluationProgramService
{
    /// <summary>Devuelve todos los programas activos de una firma ordenados por AccountSize.</summary>
    Task<IReadOnlyList<EvaluationProgramDto>> GetByFirmAsync(Guid firmId, CancellationToken ct = default);

    /// <summary>Devuelve un programa concreto (activo o inactivo) por su Id.</summary>
    Task<EvaluationProgramDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Crea un nuevo programa de evaluación. Si ya existe un programa activo con el mismo
    /// nombre para la firma, lo marca inactivo antes de insertar el nuevo (versionado).
    /// </summary>
    Task<Guid> CreateAsync(UpsertEvaluationProgramRequest request, CancellationToken ct = default);

    /// <summary>
    /// Actualiza un programa existente aplicando el patrón de versionado: marca el programa
    /// actual como inactivo y crea uno nuevo con <c>EffectiveFrom = hoy</c>.
    /// Devuelve el Id del nuevo programa creado.
    /// </summary>
    Task<Guid> UpdateAsync(Guid id, UpsertEvaluationProgramRequest request, CancellationToken ct = default);

    /// <summary>
    /// Marca un programa como inactivo (soft delete). No elimina el registro para conservar
    /// el historial en cuentas vinculadas.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
