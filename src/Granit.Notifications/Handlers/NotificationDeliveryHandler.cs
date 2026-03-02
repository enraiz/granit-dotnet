using System.Diagnostics;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Granit.Notifications.Exceptions;
using Granit.Notifications.Messages;
using Granit.Timing;
using Microsoft.Extensions.Logging;

namespace Granit.Notifications.Handlers;

/// <summary>
/// Wolverine handler that delivers a <see cref="DeliverNotificationCommand"/> via
/// the appropriate <see cref="INotificationChannel"/>.
/// </summary>
public sealed class NotificationDeliveryHandler(
    IEnumerable<INotificationChannel> channels,
    INotificationDeliveryStore deliveryStore,
    IClock clock,
    ILogger<NotificationDeliveryHandler> logger)
{
    /// <summary>
    /// Routes delivery to the matching channel. Channels not registered are skipped
    /// with a warning (NestJS graceful degradation pattern).
    /// </summary>
    public async Task HandleAsync(DeliverNotificationCommand command, CancellationToken cancellationToken)
    {
        INotificationChannel? channel = channels.FirstOrDefault(c => c.Name == command.ChannelName);

        if (channel is null)
        {
            logger.LogWarning(
                "Notification channel '{ChannelName}' is not registered — skipping delivery {DeliveryId} for notification {NotificationId}",
                command.ChannelName, command.DeliveryId, command.NotificationId);
            return;
        }

        NotificationDeliveryContext context = new()
        {
            NotificationId = command.NotificationId,
            DeliveryId = command.DeliveryId,
            NotificationTypeName = command.NotificationTypeName,
            Severity = command.Severity,
            RecipientUserId = command.RecipientUserId,
            Data = command.Data,
            RelatedEntity = command.RelatedEntity,
            TenantId = command.TenantId,
            OccurredAt = command.OccurredAt,
            Culture = command.Culture,
        };

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            await channel.SendAsync(context, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            await deliveryStore.RecordAsync(new NotificationDeliveryAttempt
            {
                Id = Guid.NewGuid(),
                DeliveryId = command.DeliveryId,
                NotificationId = command.NotificationId,
                NotificationTypeName = command.NotificationTypeName,
                ChannelName = command.ChannelName,
                RecipientUserId = command.RecipientUserId,
                TenantId = command.TenantId,
                OccurredAt = clock.Now,
                DurationMs = stopwatch.ElapsedMilliseconds,
                IsSuccess = true,
            }, cancellationToken).ConfigureAwait(false);

            logger.LogDebug(
                "Notification delivered via '{ChannelName}' for delivery {DeliveryId} notification {NotificationId}",
                command.ChannelName, command.DeliveryId, command.NotificationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();

            await deliveryStore.RecordAsync(new NotificationDeliveryAttempt
            {
                Id = Guid.NewGuid(),
                DeliveryId = command.DeliveryId,
                NotificationId = command.NotificationId,
                NotificationTypeName = command.NotificationTypeName,
                ChannelName = command.ChannelName,
                RecipientUserId = command.RecipientUserId,
                TenantId = command.TenantId,
                OccurredAt = clock.Now,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
                IsSuccess = false,
            }, cancellationToken).ConfigureAwait(false);

            logger.LogWarning(ex,
                "Notification delivery failed via '{ChannelName}' for delivery {DeliveryId} notification {NotificationId}",
                command.ChannelName, command.DeliveryId, command.NotificationId);

            throw new NotificationDeliveryException(
                $"Failed to deliver notification {command.NotificationId} via {command.ChannelName}", ex);
        }
    }
}
