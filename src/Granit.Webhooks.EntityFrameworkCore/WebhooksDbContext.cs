using Granit.Webhooks.Domain;
using Granit.Webhooks.EntityFrameworkCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Granit.Webhooks.EntityFrameworkCore;

/// <summary>
/// EF Core DbContext for the Granit.Webhooks persistence layer.
/// </summary>
/// <remarks>
/// Can be used as a standalone context or integrated into an existing application DbContext
/// by applying <see cref="WebhookSubscriptionConfiguration"/> and
/// <see cref="WebhookDeliveryAttemptConfiguration"/> in the application's <c>OnModelCreating</c>.
/// </remarks>
public sealed class WebhooksDbContext(DbContextOptions<WebhooksDbContext> options) : DbContext(options)
{
    /// <summary>Webhook subscriptions.</summary>
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();

    /// <summary>Immutable HDS audit trail of delivery attempts.</summary>
    public DbSet<WebhookDeliveryAttempt> WebhookDeliveryAttempts => Set<WebhookDeliveryAttempt>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new WebhookSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new WebhookDeliveryAttemptConfiguration());
    }
}
