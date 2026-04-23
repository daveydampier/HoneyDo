using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HoneyDo.Common.Services;

public class SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = config["Email:Smtp:Host"];

        // If no SMTP host is configured, log the full email to the console so
        // developers can grab the invite link without needing a real mail server.
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogInformation(
                "📧 [DEV — no SMTP configured] To: {To} | Subject: {Subject}\n{Body}",
                to, subject, htmlBody);
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
