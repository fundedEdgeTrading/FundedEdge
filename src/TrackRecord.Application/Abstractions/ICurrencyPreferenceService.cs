using TrackRecord.Domain.Enums;

namespace TrackRecord.Application.Abstractions;

/// <summary>Persiste la divisa de visualización elegida por el usuario (USD por defecto).</summary>
public interface ICurrencyPreferenceService
{
    Task<Currency> GetAsync(CancellationToken ct = default);
    Task SetAsync(Currency currency, CancellationToken ct = default);
}
