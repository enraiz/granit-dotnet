using System.Diagnostics;
using Amazon.SimpleEmailV2.Model;
using Granit.Notifications.Email.Ses.Diagnostics;
using Granit.Notifications.Email.Ses.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.Email.Ses.Internal;

/// <summary>
/// <see cref="IEmailSender"/> implementation using Amazon SES v2.
/// Registered as Keyed Service with key "Ses".
/// </summary>
internal sealed partial class SesEmailSender(
    IOptions<SesOptions> options,
    ILogger<SesEmailSender> logger,
    Func<ISesTransport>? transportFactory = null) : IEmailSender
{
    private readonly Func<ISesTransport> _transportFactory =
        transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        SesOptions ses = options.Value;

        using Activity? activity = NotificationsEmailSesActivitySource.Source.StartActivity(
            NotificationsEmailSesActivitySource.Operations.SendEmail);
        activity?.SetTag(NotificationsEmailSesActivitySource.Tags.Region, ses.Region);

        string fromAddress = message.FromOverride ?? ses.FromAddress ?? "noreply@localhost";

        EmailContent content = new()
        {
            Simple = new Message
            {
                Subject = new Content { Data = message.Subject },
                Body = new Body
                {
                    Html = new Content { Data = message.HtmlBody },
                },
            },
        };

        if (message.PlainTextBody is not null)
        {
            content.Simple.Body.Text = new Content { Data = message.PlainTextBody };
        }

        SendEmailRequest request = new()
        {
            FromEmailAddress = fromAddress,
            Destination = new Destination { ToAddresses = [message.To] },
            Content = content,
        };

        if (ses.ConfigurationSetName is not null)
        {
            request.ConfigurationSetName = ses.ConfigurationSetName;
        }

        using ISesTransport transport = _transportFactory();
        await transport.SendEmailAsync(request, cancellationToken).ConfigureAwait(false);

        LogEmailSent(message.To, ses.Region);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SES email sent to {Recipient} via {Region}")]
    private partial void LogEmailSent(string recipient, string region);
}
