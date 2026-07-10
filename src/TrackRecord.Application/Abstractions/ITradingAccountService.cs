using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Application.Abstractions;

public interface ITradingAccountService
{
    Task<IReadOnlyList<TradingAccountListItemDto>> GetAllAsync(AccountStage? stageFilter = null, Guid? propFirmId = null, CancellationToken ct = default);
    Task<TradingAccountDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Guid> CreateAsync(CreateTradingAccountRequest request, CancellationToken ct = default);
    Task TransitionStageAsync(TransitionAccountStageRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Actualiza la plataforma/ID externo de una cuenta ya creada (condiciona el tutorial de importación de CSV).</summary>
    Task UpdateConnectionAsync(UpdateAccountConnectionRequest request, CancellationToken ct = default);

    /// <summary>Renombra una cuenta (su DisplayName). Solo el dueño de la cuenta puede hacerlo.</summary>
    Task RenameAsync(RenameAccountRequest request, CancellationToken ct = default);

    Task<Guid> AddCostAsync(AddAccountCostRequest request, CancellationToken ct = default);
    Task RemoveCostAsync(Guid costId, CancellationToken ct = default);

    Task<Guid> AddPayoutAsync(AddPayoutRequest request, CancellationToken ct = default);
    Task RemovePayoutAsync(Guid payoutId, CancellationToken ct = default);

    Task<Guid> AddTradeAsync(CreateTradeRequest request, CancellationToken ct = default);
    Task DeleteTradeAsync(Guid tradeId, CancellationToken ct = default);
}
