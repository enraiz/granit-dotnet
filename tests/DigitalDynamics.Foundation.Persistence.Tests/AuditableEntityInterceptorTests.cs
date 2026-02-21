// =============================================================================
// Tests - AuditableEntityInterceptor
// =============================================================================
// Vérifie que les champs d'audit HDS sont correctement remplis
// lors de la création et modification des entités.
//
// Approche : on enregistre l'intercepteur dans le DbContext et on appelle
// SaveChangesAsync directement, ce qui déclenche l'intercepteur naturellement.
// IClock est mocké pour des assertions exactes (pas de BeCloseTo).
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

public sealed class AuditableEntityInterceptorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
    private static readonly Guid FixedGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");

    private readonly ICurrentUserService _currentUserService;
    private readonly IClock _clock;
    private readonly IGuidGenerator _guidGenerator;

    public AuditableEntityInterceptorTests()
    {
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns("user-test-123");

        _clock = Substitute.For<IClock>();
        _clock.Now.Returns(FixedNow);

        _guidGenerator = Substitute.For<IGuidGenerator>();
        _guidGenerator.Create().Returns(FixedGuid);
    }

    [Fact]
    public async Task SaveChangesAsync_OnAdd_SetsCreatedFields()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = new TestEntity { Name = "Test" };
        context.TestEntities.Add(entity);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        entity.CreatedAt.Should().Be(FixedNow);
        entity.CreatedBy.Should().Be("user-test-123");
        entity.Id.Should().Be(FixedGuid);
    }

    [Fact]
    public async Task SaveChangesAsync_OnModify_SetsModifiedFields()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            CreatedAt = FixedNow.AddDays(-1),
            CreatedBy = "original-user"
        };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Modifier l'entité
        entity.Name = "Modified";
        context.Entry(entity).State = EntityState.Modified;

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        entity.ModifiedAt.Should().Be(FixedNow);
        entity.ModifiedBy.Should().Be("user-test-123");
    }

    [Fact]
    public async Task SaveChangesAsync_OnModify_DoesNotOverwriteCreatedFields()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Original"
        };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Capturer les valeurs de création posées par l'intercepteur lors du Add
        var originalCreatedAt = entity.CreatedAt;
        var originalCreatedBy = entity.CreatedBy;

        // Avancer le temps pour le Modify
        _clock.Now.Returns(FixedNow.AddHours(1));

        // Modifier l'entité
        entity.Name = "Modified";
        context.Entry(entity).State = EntityState.Modified;

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — les champs de création ne doivent pas être modifiés
        entity.CreatedAt.Should().Be(originalCreatedAt);
        entity.CreatedBy.Should().Be(originalCreatedBy);
        // Mais ModifiedAt doit refléter le nouveau temps
        entity.ModifiedAt.Should().Be(FixedNow.AddHours(1));
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutUser_UsesSystem()
    {
        // Arrange
        _currentUserService.UserId.Returns((string?)null);
        await using var context = CreateContext();
        var entity = new TestEntity { Name = "Test" };
        context.TestEntities.Add(entity);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        entity.CreatedBy.Should().Be("system");
    }

    private TestDbContext CreateContext()
    {
        var interceptor = new AuditableEntityInterceptor(_currentUserService, _clock, _guidGenerator);
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        return new TestDbContext(options);
    }

    private sealed class TestEntity : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ValueGeneratedNever : l'intercepteur gere la generation des GUID
            modelBuilder.Entity<TestEntity>().Property(e => e.Id).ValueGeneratedNever();
        }
    }
}
