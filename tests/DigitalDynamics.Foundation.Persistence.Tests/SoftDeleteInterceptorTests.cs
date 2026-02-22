// =============================================================================
// Tests - SoftDeleteInterceptor
// =============================================================================
// Verifies that physical deletion is converted to logical deletion
// for ISoftDeletable entities (GDPR compliance).
//
// Approach: the interceptor is registered in the DbContext and
// SaveChangesAsync is called directly. IClock is mocked for exact assertions.
// =============================================================================

using DigitalDynamics.Foundation.Core.Domain;
using DigitalDynamics.Foundation.Guids;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.Persistence.Interceptors;
using DigitalDynamics.Foundation.Security;
using DigitalDynamics.Foundation.Timing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.Persistence.Tests;

public sealed class SoftDeleteInterceptorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly ICurrentUserService _currentUserService;
    private readonly IClock _clock;

    public SoftDeleteInterceptorTests()
    {
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns("user-test-123");

        _clock = Substitute.For<IClock>();
        _clock.Now.Returns(FixedNow);
    }

    [Fact]
    public async Task SaveChangesAsync_OnDelete_ConvertToSoftDelete()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        TestSoftDeletableEntity entity = new()
        {
            Id = Guid.NewGuid(),
            Name = "ToDelete",
            CreatedAt = FixedNow.AddDays(-1),
            CreatedBy = "user-test-123"
        };
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Delete the entity
        context.Entities.Remove(entity);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — the entity is soft-deleted (not physically removed)
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(FixedNow);
        entity.DeletedBy.Should().Be("user-test-123");

<<<<<<< HEAD
        // Verify the entity still exists in the database (not physically deleted)
        var count = await context.Entities.IgnoreQueryFilters().CountAsync(TestContext.Current.CancellationToken);
=======
        // Vérifier que l'entité existe encore en base (pas supprimée physiquement)
        int count = await context.Entities.IgnoreQueryFilters().CountAsync(TestContext.Current.CancellationToken);
>>>>>>> feature/settings-module
        count.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_OnModify_DoesNotTriggerSoftDelete()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        TestSoftDeletableEntity entity = new()
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            CreatedAt = FixedNow.AddDays(-1),
            CreatedBy = "user-test-123"
        };
        context.Entities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Modify the entity (not delete)
        entity.Name = "Modified";

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — no soft delete
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
    }

    private TestDbContext CreateContext()
    {
        IGuidGenerator guidGenerator = Substitute.For<IGuidGenerator>();
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        AuditedEntityInterceptor auditInterceptor = new(_currentUserService, _clock, guidGenerator, currentTenant);
        SoftDeleteInterceptor softDeleteInterceptor = new(_currentUserService, _clock);
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(auditInterceptor, softDeleteInterceptor)
            .Options;
        return new TestDbContext(options);
    }

    private sealed class TestSoftDeletableEntity : FullAuditedEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TestSoftDeletableEntity> Entities => Set<TestSoftDeletableEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<TestSoftDeletableEntity>().Property(e => e.Id).ValueGeneratedNever();
    }
}
