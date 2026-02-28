using Granit.Notifications.Abstractions;
using Granit.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Granit.Notifications.EntityFrameworkCore;

/// <summary>
/// EF Core SaveChanges interceptor that detects modifications on <see cref="ITrackedEntity"/>
/// and publishes notifications to entity followers (Odoo-style auto-tracking).
/// </summary>
public sealed class EntityTrackingInterceptor(
    INotificationPublisher notificationPublisher,
    IClock clock) : SaveChangesInterceptor
{
    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        List<EntityStateChange> changes = DetectChanges(eventData.Context);

        InterceptionResult<int> baseResult = await base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (EntityStateChange change in changes)
        {
            await notificationPublisher.PublishToEntityFollowersAsync(
                new EntityStateChangedNotificationType(change.NotificationTypeName, change.Severity),
                new EntityStateChangedData
                {
                    EntityType = change.EntityType,
                    EntityId = change.EntityId,
                    PropertyName = change.PropertyName,
                    OldValue = change.OldValue,
                    NewValue = change.NewValue,
                    ChangedAt = clock.Now,
                },
                new EntityReference(change.EntityType, change.EntityId),
                cancellationToken);
        }

        return baseResult;
    }

    private static List<EntityStateChange> DetectChanges(DbContext context)
    {
        List<EntityStateChange> changes = [];

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            Type entityType = entry.Entity.GetType();

            if (!entityType.GetInterfaces().Any(i => i == typeof(ITrackedEntity)))
            {
                continue;
            }

            // Use reflection to access static abstract members
            System.Reflection.PropertyInfo? entityTypeNameProp = entityType.GetProperty("EntityTypeName", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
            System.Reflection.PropertyInfo? trackedPropsProp = entityType.GetProperty("TrackedProperties", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);

            if (entityTypeNameProp is null || trackedPropsProp is null)
            {
                continue;
            }

            string entityTypeName = (string)entityTypeNameProp.GetValue(null)!;
            IReadOnlyDictionary<string, TrackedPropertyConfig> trackedProperties =
                (IReadOnlyDictionary<string, TrackedPropertyConfig>)trackedPropsProp.GetValue(null)!;

            ITrackedEntity trackedEntity = (ITrackedEntity)entry.Entity;
            string entityId = trackedEntity.GetEntityId();

            foreach (PropertyEntry property in entry.Properties)
            {
                if (property.IsModified && trackedProperties.TryGetValue(property.Metadata.Name, out TrackedPropertyConfig? config))
                {
                    changes.Add(new EntityStateChange
                    {
                        EntityType = entityTypeName,
                        EntityId = entityId,
                        PropertyName = property.Metadata.Name,
                        OldValue = property.OriginalValue?.ToString(),
                        NewValue = property.CurrentValue?.ToString(),
                        NotificationTypeName = config.NotificationTypeName,
                        Severity = config.Severity,
                    });
                }
            }
        }

        return changes;
    }

    private sealed record EntityStateChange
    {
        public required string EntityType { get; init; }
        public required string EntityId { get; init; }
        public required string PropertyName { get; init; }
        public string? OldValue { get; init; }
        public string? NewValue { get; init; }
        public required string NotificationTypeName { get; init; }
        public NotificationSeverity Severity { get; init; }
    }

    /// <summary>
    /// Internal notification type used for auto-tracked property changes.
    /// </summary>
    private sealed class EntityStateChangedNotificationType(string name, NotificationSeverity severity) : NotificationType<EntityStateChangedData>
    {
        /// <inheritdoc/>
        public override string Name => name;

        /// <inheritdoc/>
        public override NotificationSeverity DefaultSeverity => severity;

        /// <inheritdoc/>
        public override IReadOnlyList<string> DefaultChannels => [NotificationChannels.InApp, NotificationChannels.SignalR];
    }
}
