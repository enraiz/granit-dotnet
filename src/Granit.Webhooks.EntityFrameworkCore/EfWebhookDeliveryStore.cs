using Granit.Timing;
using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Domain;
using Granit.Webhooks.Messages;
using Microsoft.EntityFrameworkCore;

namespace Granit.Webhooks.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of <see cref="IWebhookDeliveryStore"/> backed by PostgreSQL.
/// </summary>
/// <remarks>
/// HDS compliance: <see cref="WebhookDeliveryAttempt"/> records are INSERT-only.
/// This store never updates or deletes them.
/// </remarks>
internal sealed class EfWebhookDeliveryStore(IDbContextFactory<WebhooksDbContext> contextFactory, IClock clock)
    : IWebhookDeliveryStore
{
    public async Task RecordSuccessAsync(
        SendWebhookCommand command,
        int httpStatusCode,
        long durationMs,
        string payloadHash,
        CancellationToken cancellationToken = default)
    {
        await using WebhooksDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.WebhookDeliveryAttempts.Add(new WebhookDeliveryAttempt
        {
            Id = Guid.NewGuid(),
            DeliveryId = command.DeliveryId,
            SubscriptionId = command.SubscriptionId,
            TenantId = command.Envelope.TenantId,
            EventType = command.Envelope.EventType,
            TargetUrl = command.TargetUrl,
            HttpStatusCode = httpStatusCode,
            PayloadHash = payloadHash,
            OccurredAt = clock.Now,
            DurationMs = durationMs,
            IsSuccess = true,
        });

        WebhookSubscription? subscription = await context.WebhookSubscriptions
            .FindAsync([command.SubscriptionId], cancellationToken).ConfigureAwait(false);

        if (subscription is not null)
        {
            subscription.LastSuccessAt = clock.Now;
            subscription.ConsecutiveFailureCount = 0;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordFailureAsync(
        SendWebhookCommand command,
        int? httpStatusCode,
        long durationMs,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        await using WebhooksDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        context.WebhookDeliveryAttempts.Add(new WebhookDeliveryAttempt
        {
            Id = Guid.NewGuid(),
            DeliveryId = command.DeliveryId,
            SubscriptionId = command.SubscriptionId,
            TenantId = command.Envelope.TenantId,
            EventType = command.Envelope.EventType,
            TargetUrl = command.TargetUrl,
            HttpStatusCode = httpStatusCode,
            PayloadHash = string.Empty,
            OccurredAt = clock.Now,
            DurationMs = durationMs,
            ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage,
            IsSuccess = false,
        });

        WebhookSubscription? subscription = await context.WebhookSubscriptions
            .FindAsync([command.SubscriptionId], cancellationToken).ConfigureAwait(false);

        if (subscription is not null)
        {
            subscription.ConsecutiveFailureCount++;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SuspendSubscriptionAsync(
        Guid subscriptionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await using WebhooksDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        WebhookSubscription? subscription = await context.WebhookSubscriptions
            .FindAsync([subscriptionId], cancellationToken).ConfigureAwait(false);

        if (subscription is null)
        {
            return;
        }

        subscription.Status = WebhookSubscriptionStatus.Suspended;
        subscription.DeactivationReason = reason;
        subscription.SuspendedAt = clock.Now;
        subscription.SuspendedBy = "system";

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
