namespace FundedEdge.Infrastructure.Email;

/// <summary>
/// Envío de emails de producto genéricos (resúmenes, alertas...), a diferencia de
/// <c>IEmailSender&lt;ApplicationUser&gt;</c> que cubre específicamente los flujos de ASP.NET Core
/// Identity (confirmación de cuenta, restablecimiento de contraseña).
/// </summary>
public interface IAppEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
