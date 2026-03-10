using Granit.Core.DataFiltering;
using Granit.Core.MultiTenancy;
using Granit.Persistence.Extensions;
using Granit.Webhooks.Domain;
using Granit.Webhooks.EntityFrameworkCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Granit.Webhooks.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core DbContext for the Granit.Webhooks persistence layer.
/// </summary>
/// <remarks>
/// Can be used as a standalone context or integrated into an existing application DbContext
/// by applying <see cref="WebhookSubscriptionConfiguration"/> and
/// <see cref="WebhookDeliveryAttemptConfiguration"/> in the application's <c>OnModelCreating</c>.
/// </remarks>
internal sealed class WebhooksDbContext(
    DbContextOptions<WebhooksDbContext> options,
    ICurrentTenant? currentTenant = null,
    IDataFilter? dataFilter = null) : DbContext(options)
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
        modelBuilder.ApplyGranitConventions(currentTenant, dataFilter);
    }
}
