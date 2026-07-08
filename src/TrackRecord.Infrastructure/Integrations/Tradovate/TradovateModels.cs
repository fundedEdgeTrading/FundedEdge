using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace TrackRecord.Infrastructure.Integrations.Tradovate;

/// <summary>Credenciales necesarias para autenticar contra la API de Tradovate (ver GUIA_IMPLEMENTACION.md §5).</summary>
public record TradovateCredentials(string Name, string Password, int Cid, string Sec, string DeviceId);

internal static class TradovateDeviceId
{
    /// <summary>
    /// Tradovate exige un deviceId estable por instalación para distinguir dispositivos.
    /// Se deriva de forma determinista del nombre de máquina si no se configura uno explícito,
    /// para no depender de persistir un archivo adicional.
    /// </summary>
    public static string Stable()
    {
        var seed = $"TrackRecord-{Environment.MachineName}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash[..16]).ToString();
    }
}

internal record TradovateAuthRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("appId")] string AppId,
    [property: JsonPropertyName("appVersion")] string AppVersion,
    [property: JsonPropertyName("cid")] int Cid,
    [property: JsonPropertyName("sec")] string Sec,
    [property: JsonPropertyName("deviceId")] string DeviceId);

internal record TradovateAuthResponse(
    [property: JsonPropertyName("accessToken")] string? AccessToken,
    [property: JsonPropertyName("expirationTime")] DateTimeOffset? ExpirationTime,
    [property: JsonPropertyName("errorText")] string? ErrorText,
    [property: JsonPropertyName("p-ticket")] string? PTicket,
    [property: JsonPropertyName("p-time")] int? PTime);

internal record TradovateAccountRaw(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("active")] bool Active);

/// <summary>Cuenta tal y como la reporta Tradovate. Se mapea a TradingAccount.ExternalAccountId.</summary>
public record TradovateAccount(long Id, string Name, bool Active);

internal record TradovateFillRaw(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("orderId")] long OrderId,
    [property: JsonPropertyName("contractId")] long ContractId,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("action")] string Action, // "Buy" | "Sell"
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("qty")] int Qty);

internal record TradovateOrderRaw(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("accountId")] long AccountId);

internal record TradovateContractRaw(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name); // p.ej. "ESH6"

/// <summary>
/// Fill resuelto (con símbolo de contrato ya mapeado), listo para convertirse en un Execution
/// del dominio con Source = Tradovate.
/// </summary>
public record TradovateFill(long Id, string Symbol, string Action, decimal Price, int Quantity, DateTimeOffset ExecutedAt);
