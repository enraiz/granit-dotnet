using Granit.Notifications.Abstractions;
using Granit.Notifications.Email.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.Email;

/// <summary>
/// Email notification channel that resolves the provider at runtime via Keyed Services.
/// </summary>
internal sealed class EmailNotificationChannel(
    IServiceProvider serviceProvider,
    IOptions<EmailChannelOptions> options,
    IRecipientResolver recipientResolver) : INotificationChannel
{
    /// <inheritdoc />
    public string Name => NotificationChannels.Email;

    /// <inheritdoc />
    public async Task SendAsync(NotificationDeliveryContext context, CancellationToken cancellationToken = default)
    {
        IEmailSender sender = serviceProvider.GetRequiredKeyedService<IEmailSender>(options.Value.Provider);

        RecipientInfo? recipient = await recipientResolver.ResolveAsync(context.RecipientUserId, cancellationToken).ConfigureAwait(false);
        if (recipient?.Email is null)
        {
            return;
        }

        string subject = $"Notification: {context.NotificationTypeName}";
        string htmlBody = $"<p>You have a new notification of type <strong>{context.NotificationTypeName}</strong>.</p>";

        await sender.SendAsync(new EmailMessage
        {
            To = recipient.Email,
            Subject = subject,
            HtmlBody = htmlBody,
            FromOverride = options.Value.SenderAddress,
        }, cancellationToken).ConfigureAwait(false);
    }
}
