// =============================================================================
// Tests - SoftDeleteInterceptor
// =============================================================================
// Vérifie que la suppression physique est convertie en suppression logique
// pour les entités ISoftDeletable (conformité RGPD).
//
// Approche : on enregistre l'intercepteur dans le DbContext et on appelle
// SaveChangesAsync directement. IClock est mocké pour des assertions exactes.
// =============================================================================

using DigitalDynamics.Foundation.Core.Domain;
using DigitalDynamics.Foundation.Guids;
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

        // Supprimer l'entité
        context.Entities.Remove(entity);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — l'entité est soft-deleted (pas physiquement supprimée)
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(FixedNow);
        entity.DeletedBy.Should().Be("user-test-123");

        // Vérifier que l'entité existe encore en base (pas supprimée physiquement)
        int count = await context.Entities.IgnoreQueryFilters().CountAsync(TestContext.Current.CancellationToken);
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

        // Modifier l'entité (pas supprimer)
        entity.Name = "Modified";

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — pas de soft delete
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
    }

    private TestDbContext CreateContext()
    {
        IGuidGenerator guidGenerator = Substitute.For<IGuidGenerator>();
        AuditedEntityInterceptor auditInterceptor = new(_currentUserService, _clock, guidGenerator);
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
