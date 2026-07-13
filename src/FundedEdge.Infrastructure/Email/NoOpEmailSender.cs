using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using FundedEdge.Infrastructure.Identity;

namespace FundedEdge.Infrastructure.Email;

/// <summary>
/// Sustituto de desarrollo cuando no hay SMTP configurado: no envía nada, solo loguea el enlace.
/// La página Account/RegisterConfirmation detecta EmailOptions.IsConfigured == false y muestra el
/// enlace de confirmación en pantalla para poder completar el flujo sin bandeja de entrada.
/// </summary>
public class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        logger.LogWarning("SMTP sin configurar — enlace de confirmación para {Email}: {Link}", email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        logger.LogWarning("SMTP sin configurar — enlace de restablecimiento para {Email}: {Link}", email, resetLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        logger.LogWarning("SMTP sin configurar — código de restablecimiento para {Email}: {Code}", email, resetCode);
        return Task.CompletedTask;
    }
}
