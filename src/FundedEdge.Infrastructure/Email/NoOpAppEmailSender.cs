using Microsoft.Extensions.Logging;

namespace FundedEdge.Infrastructure.Email;

/// <summary>
/// Sustituto de desarrollo cuando no hay SMTP configurado: no envía nada. A diferencia de
/// NoOpEmailSender (Identity), aquí no hace falta reconstruir el contenido en pantalla — lo que
/// se envía (resúmenes, alertas) ya es visible en la app en todo momento.
/// </summary>
public class NoOpAppEmailSender(ILogger<NoOpAppEmailSender> logger) : IAppEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogDebug("SMTP sin configurar — no se envía el email \"{Subject}\" a {To}.", subject, to);
        return Task.CompletedTask;
    }
}
