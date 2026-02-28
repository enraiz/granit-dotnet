using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Granit.Notifications.Email.Smtp;

/// <summary>
/// <see cref="IEmailSender"/> implementation using MailKit SMTP.
/// Registered as Keyed Service with key "Smtp".
/// </summary>
internal sealed class MailKitEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        SmtpOptions smtp = options.Value;

        MimeMessage mimeMessage = new();
        mimeMessage.From.Add(new MailboxAddress(
            message.FromOverride ?? smtp.Username ?? "noreply",
            message.FromOverride ?? smtp.Username ?? "noreply@localhost"));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        BodyBuilder bodyBuilder = new()
        {
            HtmlBody = message.HtmlBody,
        };

        if (message.PlainTextBody is not null)
        {
            bodyBuilder.TextBody = message.PlainTextBody;
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        using SmtpClient client = new();
        SecureSocketOptions socketOptions = smtp.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, ct);

        if (smtp.Username is not null && smtp.Password is not null)
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password, ct);
        }

        await client.SendAsync(mimeMessage, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}
