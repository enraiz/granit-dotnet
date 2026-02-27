using FluentAssertions;
using Granit.Core.Domain;
using Xunit;

namespace Granit.Core.Tests.Domain;

public sealed class FullAuditedEntityTests
{
    private sealed class TestEntity : FullAuditedEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        TestEntity entity = new();

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void SoftDelete_SetsAllProperties()
    {
        DateTimeOffset deletedAt = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        TestEntity entity = new()
        {
            IsDeleted = true,
            DeletedAt = deletedAt,
            DeletedBy = "user-123",
        };

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(deletedAt);
        entity.DeletedBy.Should().Be("user-123");
    }

    [Fact]
    public void ImplementsISoftDeletable() =>
        new TestEntity().Should().BeAssignableTo<ISoftDeletable>();

    [Fact]
    public void InheritsAuditedEntity() =>
        new TestEntity().Should().BeAssignableTo<AuditedEntity>();

    [Fact]
    public void AuditProperties_AreAccessible()
    {
        DateTimeOffset fixedDate = new(2026, 2, 20, 14, 0, 0, TimeSpan.Zero);
        TestEntity entity = new()
        {
            CreatedAt = fixedDate,
            CreatedBy = "admin",
            ModifiedAt = fixedDate,
            ModifiedBy = "admin",
        };

        entity.CreatedAt.Should().Be(fixedDate);
        entity.CreatedBy.Should().Be("admin");
        entity.ModifiedAt.Should().Be(fixedDate);
        entity.ModifiedBy.Should().Be("admin");
    }
}
