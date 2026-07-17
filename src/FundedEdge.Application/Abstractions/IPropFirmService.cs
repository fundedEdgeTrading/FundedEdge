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

    /// <summary>
    /// Actualiza las URLs fuente (una por línea) que <c>IFirmRulesSyncService</c> descarga para
    /// reextraer el reglamento de la firma. No toca <see cref="PropFirmDto.RulesMarkdown"/> ni
    /// dispara ninguna sincronización — solo guarda dónde debe mirar la próxima vez.
    /// </summary>
    Task UpdateRulesSourceUrlsAsync(Guid firmId, string? rulesSourceUrls, CancellationToken ct = default);
}
