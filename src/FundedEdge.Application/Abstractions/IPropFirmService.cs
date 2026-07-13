using FundedEdge.Application.Dtos;

namespace FundedEdge.Application.Abstractions;

public interface IPropFirmService
{
    Task<IReadOnlyList<PropFirmDto>> GetAllAsync(CancellationToken ct = default);
    Task<PropFirmDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateAsync(UpsertPropFirmRequest request, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpsertPropFirmRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Tiempo de pago por firma (mediana y P90 en días, solicitado→cobrado), agregado anónimo de
    /// todos los usuarios de la instancia. Las firmas sin muestra suficiente no aparecen.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, FirmPayoutSpeedDto>> GetPayoutSpeedAsync(CancellationToken ct = default);
}
