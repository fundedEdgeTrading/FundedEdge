using FundedEdge.Application.Dtos;

namespace FundedEdge.Application.Abstractions;

public interface IPropFirmService
{
    Task<IReadOnlyList<PropFirmDto>> GetAllAsync(CancellationToken ct = default);
    Task<PropFirmDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateAsync(UpsertPropFirmRequest request, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpsertPropFirmRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
