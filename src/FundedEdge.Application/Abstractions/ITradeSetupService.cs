using FundedEdge.Application.Dtos;

namespace FundedEdge.Application.Abstractions;

/// <summary>Gestiona los setups de entrada que cada usuario define para etiquetar sus trades.</summary>
public interface ITradeSetupService
{
    Task<IReadOnlyList<TradeSetupDto>> GetAllAsync(CancellationToken ct = default);
    Task<Guid> CreateAsync(string name, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
