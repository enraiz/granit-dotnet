using Granit.Notifications.Abstractions;
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
    public async Task SendAsync(NotificationDeliveryContext context, CancellationToken ct = default)
    {
        IEmailSender sender = serviceProvider.GetRequiredKeyedService<IEmailSender>(options.Value.Provider);

        RecipientInfo? recipient = await recipientResolver.ResolveAsync(context.RecipientUserId, ct);
        if (recipient?.Email is null)
        {
            return;
        }

        // TODO: integrate with Granit.Templating.Scriban for template rendering when available
        string subject = $"Notification: {context.NotificationTypeName}";
        string htmlBody = $"<p>You have a new notification of type <strong>{context.NotificationTypeName}</strong>.</p>";

        await sender.SendAsync(new EmailMessage
        {
            To = recipient.Email,
            Subject = subject,
            HtmlBody = htmlBody,
            FromOverride = options.Value.SenderAddress,
        }, ct);
    }
}
