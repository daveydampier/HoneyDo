using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HoneyDo.Common.Services;

public class SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = config["Email:Smtp:Host"];

        // If no SMTP host is configured, avoid logging private email contents.
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogInformation(
                "📧 [DEV — no SMTP configured] Email suppressed. Subject: {Subject} | BodyLength: {BodyLength}",
                subject, htmlBody?.Length ?? 0);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            config["Email:FromName"] ?? "HoneyDo",
            config["Email:FromAddress"] ?? "noreply@honeydo.app"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            host,
            int.Parse(config["Email:Smtp:Port"] ?? "587"),
            SecureSocketOptions.StartTls,
            ct);

        var username = config["Email:Smtp:Username"];
        if (!string.IsNullOrWhiteSpace(username))
            await client.AuthenticateAsync(username, config["Email:Smtp:Password"] ?? string.Empty, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
