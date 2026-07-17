namespace FundedEdge.Application.Abstractions;

/// <summary>
/// Cliente de recuperación web (Nimble). Su única responsabilidad (SRP) es descargar y desbloquear
/// una URL pública y devolver su contenido como texto (Markdown/HTML) para que la IA extraiga las
/// reglas después. No interpreta ni estructura el contenido.
/// </summary>
public interface INimbleClient
{
    /// <summary>True si hay credencial de Nimble configurada.</summary>
    bool IsConfigured { get; }

    /// <summary>Descarga <paramref name="url"/> y devuelve su contenido como texto.</summary>
    Task<string> FetchContentAsync(string url, CancellationToken ct = default);
}
