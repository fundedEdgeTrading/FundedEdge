using Microsoft.AspNetCore.DataProtection;
using FundedEdge.Infrastructure.Settings;

namespace FundedEdge.Application.Tests;

/// <summary>
/// El almacén clave/valor cifrado se conserva tras eliminar las integraciones por API: hoy lo
/// usa CurrencyPreferenceService para la divisa de visualización por usuario.
/// </summary>
public class IntegrationSettingsStoreTests
{
    private static DataProtectedIntegrationSettingsStore BuildStore(InMemoryDbContextFactory factory) =>
        new(factory, new EphemeralDataProtectionProvider());

    [Fact]
    public async Task SetThenGet_RoundTripsThroughEncryption()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);

        await store.SetAsync("user-1:Ui:Currency", "Eur");
        var value = await store.GetAsync("user-1:Ui:Currency");

        Assert.Equal("Eur", value);
    }

    [Fact]
    public async Task SetWithNullValue_DeletesKey()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var store = BuildStore(factory);

        await store.SetAsync("user-1:Ui:Currency", "Eur");
        await store.SetAsync("user-1:Ui:Currency", null);

        Assert.Null(await store.GetAsync("user-1:Ui:Currency"));
    }
}
