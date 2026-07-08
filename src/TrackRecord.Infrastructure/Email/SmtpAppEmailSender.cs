using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace TrackRecord.Infrastructure.Email;

/// <summary>Envío real por SMTP (MailKit). Solo se registra en DI cuando EmailOptions.IsConfigured.</summary>
public class SmtpAppEmailSender(EmailOptions options, ILogger<SmtpAppEmailSender> logger) : IAppEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (options.SmtpHost is null || options.From is null)
        {
            throw new InvalidOperationException("SmtpAppEmailSender requiere Email:SmtpHost y Email:From configurados.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        // StartTlsWhenAvailable cubre tanto 587 (STARTTLS) como servidores locales de desarrollo.
        await client.ConnectAsync(options.SmtpHost, options.SmtpPort, SecureSocketOptions.StartTlsWhenAvailable, ct);
        if (!string.IsNullOrWhiteSpace(options.SmtpUser))
        {
            await client.AuthenticateAsync(options.SmtpUser, options.SmtpPassword ?? "", ct);
        }
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);

        logger.LogInformation("Email \"{Subject}\" enviado a {To}.", subject, to);
    }
}
