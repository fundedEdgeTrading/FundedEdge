using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FundedEdge.Application.Abstractions;
using FundedEdge.Infrastructure.Settings;

namespace FundedEdge.Infrastructure.Integrations.Nimble;

/// <summary>
/// Implementación de <see cref="INimbleClient"/> sobre la Web API REST de Nimble mediante un
/// HttpClient tipado. No existe SDK oficial de Nimble para .NET: se consume el endpoint real-time
/// directamente (autenticación Basic con la credencial base64 del panel). Devuelve el contenido de
/// la página; si la respuesta viene envuelta en JSON, extrae el campo de contenido más probable.
/// </summary>
public sealed class NimbleClient(HttpClient http, NimbleOptions options) : INimbleClient
{
    private static readonly string[] ContentFields = ["markdown", "html_content", "content", "body", "text"];

    public bool IsConfigured => options.IsConfigured;

    public async Task<string> FetchContentAsync(string url, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Nimble no está configurado. Define Nimble:ApiKey (o NIMBLE_API_KEY).");
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("La URL no puede estar vacía.", nameof(url));

        var payload = new { url, method = "GET", render = true, country = "US", format = "json" };
        using var request = new HttpRequestMessage(HttpMethod.Post, "realtime/web")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", options.ApiKey);

        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nimble devolvió {(int)response.StatusCode} para {url}: {Truncate(body, 500)}");

        return ExtractContent(body);
    }

    /// <summary>Si el cuerpo es un JSON con un campo de contenido conocido, lo devuelve; si no, el cuerpo tal cual.</summary>
    private static string ExtractContent(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || !body.TrimStart().StartsWith('{'))
            return body;

        try
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var field in ContentFields)
            {
                if (doc.RootElement.TryGetProperty(field, out var value) &&
                    value.ValueKind == JsonValueKind.String)
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) return text!;
                }
            }
        }
        catch (JsonException)
        {
            // No era JSON válido: se devuelve el cuerpo original.
        }

        return body;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
