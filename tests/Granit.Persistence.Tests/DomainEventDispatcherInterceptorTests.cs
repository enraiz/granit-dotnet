using Granit.Core.Domain;
using Granit.Core.Events;
using Granit.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Persistence.Tests;

public sealed class DomainEventDispatcherInterceptorTests
{
    private readonly IDomainEventDispatcher _dispatcher;

    public DomainEventDispatcherInterceptorTests()
    {
        _dispatcher = Substitute.For<IDomainEventDispatcher>();
        _dispatcher.DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task SaveChangesAsync_DispatchesDomainEvents_AfterSave()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        TestAggregate aggregate = new();
        aggregate.DoSomething();
        context.Aggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<IReadOnlyList<IDomainEvent>>(events => events.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_ClearsEventsFromAggregate_AfterCollecting()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        TestAggregate aggregate = new();
        aggregate.DoSomething();
        context.Aggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — events cleared during collection phase
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_NoAggregatesWithEvents_DoesNotDispatch()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        TestAggregate aggregate = new(); // no events raised
        context.Aggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await _dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<IReadOnlyList<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_MultipleAggregates_CollectsAllEvents()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        TestAggregate a1 = new() { Id = Guid.NewGuid() };
        TestAggregate a2 = new() { Id = Guid.NewGuid() };
        a1.DoSomething();
        a2.DoSomething();
        a2.DoSomething();
        context.Aggregates.AddRange(a1, a2);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — 3 events total (1 from a1 + 2 from a2)
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<IReadOnlyList<IDomainEvent>>(events => events.Count == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_MixedEntitiesAndAggregates_OnlyCollectsFromAggregates()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        TestAggregate aggregate = new();
        aggregate.DoSomething();
        TestPlainEntity plain = new() { Name = "plain" };
        context.Aggregates.Add(aggregate);
        context.PlainEntities.Add(plain);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — only the aggregate's event is dispatched
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<IReadOnlyList<IDomainEvent>>(events => events.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_AuditedAggregateRoot_DispatchesEvents()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        TestAuditedAggregate aggregate = new();
        aggregate.DoSomething();
        context.AuditedAggregates.Add(aggregate);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<IReadOnlyList<IDomainEvent>>(events => events.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_SecondSave_DoesNotRedispatchPreviousEvents()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        TestAggregate aggregate = new();
        aggregate.DoSomething();
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dispatcher.ClearReceivedCalls();

        // Act — second save without new events
        aggregate.Name = "modified";
        context.Entry(aggregate).State = EntityState.Modified;
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert — no dispatch on second save
        await _dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<IReadOnlyList<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private TestDbContext CreateContext()
    {
        DomainEventDispatcherInterceptor interceptor = new(_dispatcher);
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        return new TestDbContext(options);
    }

    // -------------------------------------------------------------------------
    // Test fixtures
    // -------------------------------------------------------------------------

    private sealed record SomethingHappened(Guid EntityId) : IDomainEvent;

    private sealed class TestAggregate : AggregateRoot
    {
        public string Name { get; set; } = string.Empty;
        public void DoSomething() => AddDomainEvent(new SomethingHappened(Id));
    }

    private sealed class TestAuditedAggregate : AuditedAggregateRoot
    {
        public string Name { get; set; } = string.Empty;
        public void DoSomething() => AddDomainEvent(new SomethingHappened(Id));
    }

    private sealed class TestPlainEntity : Entity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext(DbContextOptions<DomainEventDispatcherInterceptorTests.TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();
        public DbSet<TestAuditedAggregate> AuditedAggregates => Set<TestAuditedAggregate>();
        public DbSet<TestPlainEntity> PlainEntities => Set<TestPlainEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>().Property(e => e.Id).ValueGeneratedNever();
            modelBuilder.Entity<TestAuditedAggregate>().Property(e => e.Id).ValueGeneratedNever();
            modelBuilder.Entity<TestPlainEntity>().Property(e => e.Id).ValueGeneratedNever();
        }
    }
}
