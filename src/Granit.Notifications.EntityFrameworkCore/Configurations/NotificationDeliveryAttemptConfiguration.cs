using Granit.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Granit.Notifications.EntityFrameworkCore.Configurations;

internal sealed class NotificationDeliveryAttemptConfiguration : IEntityTypeConfiguration<NotificationDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryAttempt> builder)
    {
        builder.ToTable("notification_delivery_attempts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NotificationTypeName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ChannelName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RecipientUserId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2048);

        builder.HasIndex(x => new { x.NotificationId, x.ChannelName })
            .HasDatabaseName("ix_notification_delivery_attempts_notification");

        builder.HasIndex(x => new { x.TenantId, x.OccurredAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_notification_delivery_attempts_audit");
    }
}
