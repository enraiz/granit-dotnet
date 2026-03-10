using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Webhooks.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="IWebhookSubscriptionReader"/> and
/// <see cref="IWebhookSubscriptionWriter"/> backed by PostgreSQL.
/// </summary>
internal sealed class EfWebhookSubscriptionStore(IDbContextFactory<WebhooksDbContext> contextFactory)
    : IWebhookSubscriptionReader, IWebhookSubscriptionWriter
{
    public async Task<IReadOnlyList<WebhookSubscription>> GetActiveSubscriptionsAsync(
        string eventType,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        await using WebhooksDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.WebhookSubscriptions
            .Where(s => s.Status == WebhookSubscriptionStatus.Active
                     && s.EventType == eventType
                     && (s.TenantId == null || s.TenantId == tenantId))
            .AsNoTracking()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<WebhookSubscription?> FindByIdAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        await using WebhooksDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.WebhookSubscriptions.FindAsync([subscriptionId], cancellationToken).ConfigureAwait(false);
    }

    public async Task DeactivateAsync(
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

        subscription.Deactivate(reason);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
