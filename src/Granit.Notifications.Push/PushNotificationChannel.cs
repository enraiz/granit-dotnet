using System.Text.Json;
using Granit.Notifications.Abstractions;
using Lib.Net.Http.WebPush;
using Microsoft.Extensions.Logging;

namespace Granit.Notifications.Push;

/// <summary>
/// <see cref="INotificationChannel"/> implementation for W3C Web Push (VAPID).
/// Sends push notifications to all browser subscriptions of a user.
/// </summary>
internal sealed class PushNotificationChannel(
    PushServiceClient pushServiceClient,
    IPushSubscriptionReader subscriptionReader,
    IPushSubscriptionWriter subscriptionWriter,
    ILogger<PushNotificationChannel> logger) : INotificationChannel
{
    /// <inheritdoc />
    public string Name => NotificationChannels.Push;

    /// <inheritdoc />
    public async Task SendAsync(NotificationDeliveryContext context, CancellationToken ct = default)
    {
        IReadOnlyList<PushSubscriptionInfo> subscriptions = await subscriptionReader.GetSubscriptionsAsync(
            context.RecipientUserId, context.TenantId, ct).ConfigureAwait(false);

        if (subscriptions.Count == 0)
        {
            logger.LogDebug(
                "No push subscriptions for user {UserId}, skipping",
                context.RecipientUserId);
            return;
        }

        PushNotificationPayload payload = new()
        {
            NotificationId = context.NotificationId,
            NotificationTypeName = context.NotificationTypeName,
            Severity = context.Severity.ToString(),
            Data = context.Data,
            OccurredAt = context.OccurredAt,
        };

        string serializedPayload = JsonSerializer.Serialize(payload);

        foreach (PushSubscriptionInfo sub in subscriptions)
        {
            Lib.Net.Http.WebPush.PushSubscription pushSubscription = new()
            {
                Endpoint = sub.Endpoint,
            };
            pushSubscription.SetKey(PushEncryptionKeyName.P256DH, sub.P256dh);
            pushSubscription.SetKey(PushEncryptionKeyName.Auth, sub.Auth);

            PushMessage pushMessage = new(serializedPayload)
            {
                Urgency = PushMessageUrgency.Normal,
            };

            try
            {
                await pushServiceClient.RequestPushMessageDeliveryAsync(pushSubscription, pushMessage, ct).ConfigureAwait(false);
            }
            catch (PushServiceClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                logger.LogInformation(
                    ex,
                    "Push subscription expired for endpoint {Endpoint}, removing",
                    sub.Endpoint);
                await subscriptionWriter.RemoveSubscriptionAsync(sub.Endpoint, context.TenantId, ct).ConfigureAwait(false);
            }
        }
    }
}
