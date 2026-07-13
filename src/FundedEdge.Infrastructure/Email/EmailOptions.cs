namespace FundedEdge.Infrastructure.Email;

/// <summary>
/// Configuración del envío de emails transaccionales (confirmación de cuenta). Como con Google
/// OAuth y Stripe, los secretos SMTP nunca van en appsettings.json versionado — solo User Secrets
/// o variables de entorno (Email:SmtpHost, Email:SmtpPort, Email:SmtpUser, Email:SmtpPassword,
/// Email:From, Email:FromName). Sin configurar, la app arranca igualmente: el enlace de
/// confirmación se muestra en pantalla tras registrarse (aceptable solo en desarrollo — en
/// producción configura SMTP para que la verificación frene el registro de bots de verdad).
/// </summary>
public sealed record EmailOptions(
    bool IsConfigured,
    string? SmtpHost,
    int SmtpPort,
    string? SmtpUser,
    string? SmtpPassword,
    string? From,
    string FromName);
