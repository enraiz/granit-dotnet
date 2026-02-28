using Granit.Notifications.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.Sms;

/// <summary>
/// SMS notification channel that resolves the provider at runtime via Keyed Services.
/// </summary>
internal sealed class SmsNotificationChannel(
    IServiceProvider serviceProvider,
    IOptions<SmsChannelOptions> options,
    IRecipientResolver recipientResolver) : INotificationChannel
{
    /// <inheritdoc />
    public string Name => NotificationChannels.Sms;

    /// <inheritdoc />
    public async Task SendAsync(NotificationDeliveryContext context, CancellationToken ct = default)
    {
        ISmsSender sender = serviceProvider.GetRequiredKeyedService<ISmsSender>(options.Value.Provider);

        RecipientInfo? recipient = await recipientResolver.ResolveAsync(context.RecipientUserId, ct);
        if (recipient?.PhoneNumber is null)
        {
            return;
        }

        string body = $"Notification: {context.NotificationTypeName}";

        await sender.SendAsync(new SmsMessage
        {
            To = recipient.PhoneNumber,
            Body = body,
            SenderId = options.Value.SenderId,
        }, ct);
    }
}
