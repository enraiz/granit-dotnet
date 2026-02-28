using Granit.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Granit.Notifications.EntityFrameworkCore.Configurations;

internal sealed class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("notification_user_notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NotificationTypeName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RecipientUserId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Data).HasColumnType("jsonb");
        builder.Property(x => x.RelatedEntityType).HasMaxLength(256);
        builder.Property(x => x.RelatedEntityId).HasMaxLength(256);

        // Paginated inbox
        builder.HasIndex(x => new { x.RecipientUserId, x.TenantId, x.State, x.CreatedAt })
            .IsDescending(false, false, false, true)
            .HasDatabaseName("ix_notification_user_notifications_inbox");

        // Activity feed Odoo-style
        builder.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId, x.TenantId, x.CreatedAt })
            .IsDescending(false, false, false, true)
            .HasDatabaseName("ix_notification_user_notifications_entity_feed");
    }
}
