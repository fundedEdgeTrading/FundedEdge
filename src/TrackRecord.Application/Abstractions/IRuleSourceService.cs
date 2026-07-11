using TrackRecord.Application.Dtos;

namespace TrackRecord.Application.Abstractions;

/// <summary>
/// CRUD de las fuentes monitorizadas de reglas (datos compartidos, gestión desde /admin).
/// La comprobación en sí la hace <see cref="IRuleSourceChecker"/>.
/// </summary>
public interface IRuleSourceService
{
    /// <summary>Todas las fuentes, ordenadas por firma y tipo.</summary>
    Task<IReadOnlyList<RuleSourceDto>> GetAllAsync(CancellationToken ct = default);

    Task<Guid> CreateAsync(UpsertRuleSourceRequest request, CancellationToken ct = default);

    Task UpdateAsync(Guid id, UpsertRuleSourceRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
