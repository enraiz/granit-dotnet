using Granit.Notifications.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.WhatsApp;

/// <summary>
/// WhatsApp notification channel that resolves the provider at runtime via Keyed Services.
/// </summary>
internal sealed class WhatsAppNotificationChannel(
    IServiceProvider serviceProvider,
    IOptions<WhatsAppChannelOptions> options,
    IRecipientResolver recipientResolver) : INotificationChannel
{
    /// <inheritdoc />
    public string Name => NotificationChannels.WhatsApp;

    /// <inheritdoc />
    public async Task SendAsync(NotificationDeliveryContext context, CancellationToken ct = default)
    {
        IWhatsAppSender sender = serviceProvider.GetRequiredKeyedService<IWhatsAppSender>(options.Value.Provider);

        RecipientInfo? recipient = await recipientResolver.ResolveAsync(context.RecipientUserId, ct);
        if (recipient?.PhoneNumber is null)
        {
            return;
        }

        await sender.SendAsync(new WhatsAppMessage
        {
            To = recipient.PhoneNumber,
            TemplateName = context.NotificationTypeName,
            Language = context.Culture ?? recipient.PreferredCulture ?? "fr",
        }, ct);
    }
}
