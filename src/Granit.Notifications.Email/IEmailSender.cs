namespace Granit.Notifications.Email;

/// <summary>
/// Abstraction for sending transactional emails. Implemented by providers
/// (MailKit SMTP, Brevo, etc.) and registered as Keyed Services.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends an email message.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
