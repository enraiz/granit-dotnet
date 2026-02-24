// =============================================================================
// Tests — RunMigrationBatchHandler
// =============================================================================
// Verifies the cascade logic, progress lifecycle, and error handling of the
// Wolverine handler without requiring a real database or message bus.
// MigrationProgressDbContext uses the EF Core InMemory provider.
// =============================================================================

using FluentAssertions;
using Granit.Persistence.Migrations.Internal;
using Granit.Persistence.Migrations.Messages;
using Granit.Timing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Granit.Persistence.Migrations.Tests;

public sealed class RunMigrationBatchHandlerTests : IDisposable
{
    private readonly MigrationProgressDbContext _progressContext;
    private readonly ITenantDbIsolator _isolator;
    private readonly IClock _clock;

    public RunMigrationBatchHandlerTests()
    {
        DbContextOptions<MigrationProgressDbContext> options =
            new DbContextOptionsBuilder<MigrationProgressDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        _progressContext = new MigrationProgressDbContext(options);
        _isolator = Substitute.For<ITenantDbIsolator>();
        _clock = Substitute.For<IClock>();
        _clock.Now.Returns(DateTimeOffset.UtcNow);
    }

    public void Dispose() => _progressContext.Dispose();

    // Builds a registry that returns the given registration for cycleId.
    private static IMigrationCycleRegistry RegistryWith(
        string cycleId, Type dbContextType, BatchMigrationDelegate migration)
    {
        IMigrationCycleRegistry registry = Substitute.For<IMigrationCycleRegistry>();
        registry.Find(cycleId).Returns(new MigrationCycleRegistration(cycleId, dbContextType, migration));
        return registry;
    }

    // Builds a service provider that has TContext registered.
    private static ServiceProvider ProviderWith<TContext>(TContext context)
        where TContext : DbContext
    {
        ServiceCollection services = new();
        services.AddSingleton(context);
        return services.BuildServiceProvider();
    }

    private RunMigrationBatchHandler BuildHandler(
        IMigrationCycleRegistry registry,
        IServiceProvider serviceProvider) =>
        new(
            registry,
            serviceProvider,
            _progressContext,
            _isolator,
            _clock,
            NullLogger<RunMigrationBatchHandler>.Instance);

    // -------------------------------------------------------------------------
    // Unknown cycle
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_UnknownCycle_ReturnsEmptyArray()
    {
        IMigrationCycleRegistry registry = Substitute.For<IMigrationCycleRegistry>();
        registry.Find(Arg.Any<string>()).Returns((MigrationCycleRegistration?)null);
        RunMigrationBatchHandler handler = BuildHandler(registry, new ServiceCollection().BuildServiceProvider());

        object[] result = await handler.HandleAsync(
            new RunMigrationBatchCommand("missing", Guid.Empty, null, 100),
            TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Already-completed cycle
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_AlreadyCompleted_ReturnsEmptyArray()
    {
        string cycleId = "completed-cycle";
        StubDbContext stubContext = CreateStubContext();
        IMigrationCycleRegistry registry = RegistryWith(
            cycleId, typeof(StubDbContext),
            (_, _, _) => Task.FromResult(new MigrationBatchResult(10, null)));
        RunMigrationBatchHandler handler = BuildHandler(registry, ProviderWith(stubContext));

        _progressContext.MigrationProgresses.Add(new MigrationProgress
        {
            Id = Guid.NewGuid(),
            CycleId = cycleId,
            Phase = MigrationPhase.Migrate,
            Status = MigrationStatus.Completed,
            TenantId = null,
        });
        await _progressContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        object[] result = await handler.HandleAsync(
            new RunMigrationBatchCommand(cycleId, Guid.Empty, null, 100),
            TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Last batch (no next cursor) → migration complete
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_LastBatch_ReturnsEmptyArrayAndMarksCompleted()
    {
        string cycleId = "last-batch";
        StubDbContext stubContext = CreateStubContext();
        DateTimeOffset completedAt = DateTimeOffset.UtcNow;
        _clock.Now.Returns(completedAt);

        IMigrationCycleRegistry registry = RegistryWith(
            cycleId, typeof(StubDbContext),
            (_, _, _) => Task.FromResult(new MigrationBatchResult(42, null)));
        RunMigrationBatchHandler handler = BuildHandler(registry, ProviderWith(stubContext));

        object[] result = await handler.HandleAsync(
            new RunMigrationBatchCommand(cycleId, Guid.Empty, null, 100),
            TestContext.Current.CancellationToken);

        result.Should().BeEmpty();

        MigrationProgress? progress = await _progressContext.MigrationProgresses
            .FirstOrDefaultAsync(p => p.CycleId == cycleId, TestContext.Current.CancellationToken);
        progress.Should().NotBeNull();
        progress!.Status.Should().Be(MigrationStatus.Completed);
        progress.ProcessedRows.Should().Be(42);
        progress.CompletedAt.Should().Be(completedAt);
    }

    // -------------------------------------------------------------------------
    // Mid-batch (has next cursor) → cascade
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_MidBatch_ReturnsCascadeCommand()
    {
        string cycleId = "mid-batch";
        StubDbContext stubContext = CreateStubContext();
        IMigrationCycleRegistry registry = RegistryWith(
            cycleId, typeof(StubDbContext),
            (_, _, _) => Task.FromResult(new MigrationBatchResult(100, "{\"lastId\":999}")));
        RunMigrationBatchHandler handler = BuildHandler(registry, ProviderWith(stubContext));

        object[] result = await handler.HandleAsync(
            new RunMigrationBatchCommand(cycleId, Guid.Empty, null, 100),
            TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        RunMigrationBatchCommand cascade = result[0].Should().BeOfType<RunMigrationBatchCommand>().Subject;
        cascade.CycleId.Should().Be(cycleId);
        cascade.Cursor.Should().Be("{\"lastId\":999}");
        cascade.BatchSize.Should().Be(100);
        cascade.TenantId.Should().Be(Guid.Empty);
    }

    // -------------------------------------------------------------------------
    // Accumulated row count across two consecutive batches
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_AccumulatesProcessedRows_AcrossBatches()
    {
        string cycleId = "accumulate";
        StubDbContext stubContext = CreateStubContext();
        int callCount = 0;

        IMigrationCycleRegistry registry = RegistryWith(
            cycleId, typeof(StubDbContext),
            (_, _, _) =>
            {
                callCount++;
                string? next = callCount == 1 ? "cursor-2" : null;
                return Task.FromResult(new MigrationBatchResult(50, next));
            });
        RunMigrationBatchHandler handler = BuildHandler(registry, ProviderWith(stubContext));

        object[] result1 = await handler.HandleAsync(
            new RunMigrationBatchCommand(cycleId, Guid.Empty, null, 100),
            TestContext.Current.CancellationToken);
        result1.Should().HaveCount(1);

        object[] result2 = await handler.HandleAsync(
            (RunMigrationBatchCommand)result1[0],
            TestContext.Current.CancellationToken);
        result2.Should().BeEmpty();

        MigrationProgress? progress = await _progressContext.MigrationProgresses
            .FirstOrDefaultAsync(p => p.CycleId == cycleId, TestContext.Current.CancellationToken);
        progress!.ProcessedRows.Should().Be(100);
        progress.Status.Should().Be(MigrationStatus.Completed);
    }

    // -------------------------------------------------------------------------
    // Tenant isolation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_NonEmptyTenantId_CallsIsolator()
    {
        string cycleId = "tenant-isolation";
        Guid tenantId = Guid.NewGuid();
        StubDbContext stubContext = CreateStubContext();
        IMigrationCycleRegistry registry = RegistryWith(
            cycleId, typeof(StubDbContext),
            (_, _, _) => Task.FromResult(new MigrationBatchResult(0, null)));
        RunMigrationBatchHandler handler = BuildHandler(registry, ProviderWith(stubContext));

        await handler.HandleAsync(
            new RunMigrationBatchCommand(cycleId, tenantId, null, 100),
            TestContext.Current.CancellationToken);

        await _isolator.Received(1).IsolateAsync(
            Arg.Any<DbContext>(), tenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmptyTenantId_DoesNotCallIsolator()
    {
        string cycleId = "no-tenant";
        StubDbContext stubContext = CreateStubContext();
        IMigrationCycleRegistry registry = RegistryWith(
            cycleId, typeof(StubDbContext),
            (_, _, _) => Task.FromResult(new MigrationBatchResult(0, null)));
        RunMigrationBatchHandler handler = BuildHandler(registry, ProviderWith(stubContext));

        await handler.HandleAsync(
            new RunMigrationBatchCommand(cycleId, Guid.Empty, null, 100),
            TestContext.Current.CancellationToken);

        await _isolator.DidNotReceive().IsolateAsync(
            Arg.Any<DbContext>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // TenantId → null mapping in progress entity
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_EmptyTenantId_StoresNullTenantIdInProgress()
    {
        string cycleId = "null-tenant";
        StubDbContext stubContext = CreateStubContext();
        IMigrationCycleRegistry registry = RegistryWith(
            cycleId, typeof(StubDbContext),
            (_, _, _) => Task.FromResult(new MigrationBatchResult(0, null)));
        RunMigrationBatchHandler handler = BuildHandler(registry, ProviderWith(stubContext));

        await handler.HandleAsync(
            new RunMigrationBatchCommand(cycleId, Guid.Empty, null, 100),
            TestContext.Current.CancellationToken);

        MigrationProgress? progress = await _progressContext.MigrationProgresses
            .FirstOrDefaultAsync(p => p.CycleId == cycleId, TestContext.Current.CancellationToken);
        progress.Should().NotBeNull();
        progress!.TenantId.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // Error handling
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_BatchThrows_MarksFailedAndRethrows()
    {
        string cycleId = "failing-batch";
        StubDbContext stubContext = CreateStubContext();
        IMigrationCycleRegistry registry = RegistryWith(
            cycleId, typeof(StubDbContext),
            (_, _, _) => throw new InvalidOperationException("boom"));
        RunMigrationBatchHandler handler = BuildHandler(registry, ProviderWith(stubContext));

        Func<Task> act = () => handler.HandleAsync(
            new RunMigrationBatchCommand(cycleId, Guid.Empty, null, 100),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

        MigrationProgress? progress = await _progressContext.MigrationProgresses
            .FirstOrDefaultAsync(p => p.CycleId == cycleId, TestContext.Current.CancellationToken);
        progress.Should().NotBeNull();
        progress!.Status.Should().Be(MigrationStatus.Failed);
        progress.Error.Should().Be("boom");
    }

    [Fact]
    public async Task HandleAsync_BatchThrowsOperationCanceled_PropagatesWithoutMarkingFailed()
    {
        string cycleId = "cancel-batch";
        StubDbContext stubContext = CreateStubContext();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        IMigrationCycleRegistry registry = RegistryWith(
            cycleId, typeof(StubDbContext),
            (_, _, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(new MigrationBatchResult(0, null));
            });
        RunMigrationBatchHandler handler = BuildHandler(registry, ProviderWith(stubContext));

        Func<Task> act = () => handler.HandleAsync(
            new RunMigrationBatchCommand(cycleId, Guid.Empty, null, 100),
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();

        // Progress should NOT be marked as Failed for cancellation.
        // Use a fresh CTS since the original one is cancelled.
        MigrationProgress? progress = await _progressContext.MigrationProgresses
            .FirstOrDefaultAsync(p => p.CycleId == cycleId, TestContext.Current.CancellationToken);
        progress.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // Error message truncation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_LongErrorMessage_TruncatesTo4000Chars()
    {
        string cycleId = "long-error";
        StubDbContext stubContext = CreateStubContext();
        string longMessage = new('x', 5000);
        IMigrationCycleRegistry registry = RegistryWith(
            cycleId, typeof(StubDbContext),
            (_, _, _) => throw new InvalidOperationException(longMessage));
        RunMigrationBatchHandler handler = BuildHandler(registry, ProviderWith(stubContext));

        try
        {
            await handler.HandleAsync(
                new RunMigrationBatchCommand(cycleId, Guid.Empty, null, 100),
                TestContext.Current.CancellationToken);
        }
        catch (InvalidOperationException) { /* expected */ }

        MigrationProgress? progress = await _progressContext.MigrationProgresses
            .FirstOrDefaultAsync(p => p.CycleId == cycleId, TestContext.Current.CancellationToken);
        progress!.Error.Should().HaveLength(4000);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static StubDbContext CreateStubContext() =>
        new(new DbContextOptionsBuilder<StubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class StubDbContext(DbContextOptions<StubDbContext> options) : DbContext(options);
}
