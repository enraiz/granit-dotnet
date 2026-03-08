using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Granit.Notifications.EntityFrameworkCore.Configurations;

internal sealed class MobilePushTokenConfiguration : IEntityTypeConfiguration<MobilePushTokenEntity>
{
    public void Configure(EntityTypeBuilder<MobilePushTokenEntity> builder)
    {
        builder.ToTable("mobile_push_tokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DeviceToken).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Platform).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.CreatedBy).HasMaxLength(256);

        // Unique constraint: one device token per tenant
        builder.HasIndex(x => new { x.DeviceToken, x.TenantId })
            .IsUnique()
            .HasDatabaseName("ix_mobile_push_tokens_device_tenant");

        // Lookup by user + tenant
        builder.HasIndex(x => new { x.UserId, x.TenantId })
            .HasDatabaseName("ix_mobile_push_tokens_user_tenant");
    }
}
