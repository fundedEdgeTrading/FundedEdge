using Microsoft.AspNetCore.Identity;
using FundedEdge.Domain.Common;
using FundedEdge.Infrastructure.Identity;

namespace FundedEdge.Infrastructure.Email;

/// <summary>
/// Emails de Identity (confirmación de cuenta, restablecimiento de contraseña) sobre el envío
/// genérico de IAppEmailSender. Solo se registra en DI cuando EmailOptions.IsConfigured.
/// </summary>
public class SmtpEmailSender(IAppEmailSender appEmailSender) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        appEmailSender.SendAsync(email,
            $"Confirma tu cuenta de {Brand.Name}",
            $"""
            <p>Hola{Salutation(user)},</p>
            <p>Confirma tu cuenta de {Brand.Name} haciendo clic en este enlace:</p>
            <p><a href="{confirmationLink}">Confirmar mi cuenta</a></p>
            <p>Si no has creado esta cuenta, ignora este mensaje.</p>
            """);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        appEmailSender.SendAsync(email,
            $"Restablece tu contraseña de {Brand.Name}",
            $"""
            <p>Hola{Salutation(user)},</p>
            <p>Restablece tu contraseña haciendo clic en este enlace:</p>
            <p><a href="{resetLink}">Restablecer contraseña</a></p>
            <p>Si no lo has pedido tú, ignora este mensaje.</p>
            """);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        appEmailSender.SendAsync(email,
            $"Código para restablecer tu contraseña de {Brand.Name}",
            $"""
            <p>Hola{Salutation(user)},</p>
            <p>Tu código para restablecer la contraseña es: <strong>{resetCode}</strong></p>
            <p>Si no lo has pedido tú, ignora este mensaje.</p>
            """);

    private static string Salutation(ApplicationUser user) =>
        string.IsNullOrWhiteSpace(user.DisplayName) ? "" : $" {user.DisplayName}";
}
