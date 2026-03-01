using Granit.Core.MultiTenancy;
using Granit.Guids;
using Granit.Templating.Store;
using Granit.Timing;
using Granit.Workflow;
using Granit.Workflow.Domain;
using Granit.Workflow.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Templating.Workflow.Tests;

public sealed class WorkflowTemplateTransitionHookTests
{
    // -------------------------------------------------------------------------
    // Test infrastructure
    // -------------------------------------------------------------------------

    private sealed class TestWorkflowDbContext(DbContextOptions<TestWorkflowDbContext> options)
        : DbContext(options), IWorkflowDbContext
    {
        public DbSet<WorkflowTransitionRecord> WorkflowTransitionRecords => Set<WorkflowTransitionRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkflowTransitionRecord>(b =>
            {
                b.HasKey(e => e.Id);
                b.Property(e => e.EntityType).HasMaxLength(200);
                b.Property(e => e.EntityId).HasMaxLength(200);
                b.Property(e => e.PreviousState).HasMaxLength(50);
                b.Property(e => e.NewState).HasMaxLength(50);
                b.Property(e => e.TransitionedBy).HasMaxLength(200);
                b.Property(e => e.Comment).HasMaxLength(2000);
            });
        }
    }

    private sealed class TestContextFactory(string dbName) : IDbContextFactory<TestWorkflowDbContext>
    {
        public TestWorkflowDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<TestWorkflowDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options);

        public Task<TestWorkflowDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(CreateDbContext());
    }

    private static string NewDb() => Guid.NewGuid().ToString();

    private static WorkflowTemplateTransitionHook<TestWorkflowDbContext> CreateHook(
        string dbName,
        IWorkflowManager<WorkflowLifecycleStatus>? workflowManager = null,
        ICurrentTenant? currentTenant = null)
    {
        IWorkflowManager<WorkflowLifecycleStatus> manager = workflowManager ?? CreateDefaultManager();
        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

        IGuidGenerator guidGenerator = Substitute.For<IGuidGenerator>();
        guidGenerator.Create().Returns(_ => Guid.NewGuid());

        ICurrentTenant tenant = currentTenant ?? CreateNullTenant();

        return new WorkflowTemplateTransitionHook<TestWorkflowDbContext>(
            manager, new TestContextFactory(dbName), clock, guidGenerator, tenant);
    }

    private static IWorkflowManager<WorkflowLifecycleStatus> CreateDefaultManager()
    {
        IWorkflowManager<WorkflowLifecycleStatus> manager = Substitute.For<IWorkflowManager<WorkflowLifecycleStatus>>();
        manager.GetAllowedTransitionsAsync(Arg.Any<WorkflowLifecycleStatus>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                WorkflowLifecycleStatus from = callInfo.Arg<WorkflowLifecycleStatus>();
                List<WorkflowTransition<WorkflowLifecycleStatus>> transitions = from switch
                {
                    WorkflowLifecycleStatus.Draft =>
                    [
                        new() { From = from, To = WorkflowLifecycleStatus.Published },
                        new() { From = from, To = WorkflowLifecycleStatus.PendingReview },
                    ],
                    WorkflowLifecycleStatus.Published =>
                    [
                        new() { From = from, To = WorkflowLifecycleStatus.Archived },
                        new() { From = from, To = WorkflowLifecycleStatus.Draft },
                    ],
                    WorkflowLifecycleStatus.PendingReview =>
                    [
                        new() { From = from, To = WorkflowLifecycleStatus.Published },
                    ],
                    _ => [],
                };
                return (IReadOnlyList<WorkflowTransition<WorkflowLifecycleStatus>>)transitions;
            });
        return manager;
    }

    private static ICurrentTenant CreateNullTenant()
    {
        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.IsAvailable.Returns(false);
        tenant.Id.Returns((Guid?)null);
        return tenant;
    }

    // -------------------------------------------------------------------------
    // IsWorkflowEnabled
    // -------------------------------------------------------------------------

    [Fact]
    public void IsWorkflowEnabled_ReturnsTrue()
    {
        WorkflowTemplateTransitionHook<TestWorkflowDbContext> hook = CreateHook(NewDb());
        hook.IsWorkflowEnabled.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // CanTransitionAsync
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(TemplateLifecycleStatus.Draft, TemplateLifecycleStatus.Published, true)]
    [InlineData(TemplateLifecycleStatus.Draft, TemplateLifecycleStatus.PendingReview, true)]
    [InlineData(TemplateLifecycleStatus.Published, TemplateLifecycleStatus.Archived, true)]
    [InlineData(TemplateLifecycleStatus.Published, TemplateLifecycleStatus.Draft, true)]
    [InlineData(TemplateLifecycleStatus.PendingReview, TemplateLifecycleStatus.Published, true)]
    [InlineData(TemplateLifecycleStatus.Archived, TemplateLifecycleStatus.Draft, false)]
    [InlineData(TemplateLifecycleStatus.Archived, TemplateLifecycleStatus.Published, false)]
    public async Task CanTransitionAsync_DelegatesToWorkflowManager(
        TemplateLifecycleStatus from, TemplateLifecycleStatus to, bool expected)
    {
        WorkflowTemplateTransitionHook<TestWorkflowDbContext> hook = CreateHook(NewDb());

        bool result = await hook.CanTransitionAsync(from, to,
            TestContext.Current.CancellationToken);

        result.ShouldBe(expected);
    }

    // -------------------------------------------------------------------------
    // OnTransitionedAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OnTransitionedAsync_CreatesWorkflowTransitionRecord()
    {
        string db = NewDb();
        WorkflowTemplateTransitionHook<TestWorkflowDbContext> hook = CreateHook(db);
        Guid revisionId = Guid.NewGuid();

        await hook.OnTransitionedAsync(
            revisionId, TemplateLifecycleStatus.Draft, TemplateLifecycleStatus.Published, "alice",
            TestContext.Current.CancellationToken);

        await using TestWorkflowDbContext ctx = new TestContextFactory(db).CreateDbContext();
        WorkflowTransitionRecord? record = await ctx.WorkflowTransitionRecords.FirstOrDefaultAsync(
            TestContext.Current.CancellationToken);

        record.ShouldNotBeNull();
        record.EntityType.ShouldBe("TemplateRevision");
        record.EntityId.ShouldBe(revisionId.ToString());
        record.PreviousState.ShouldBe("Draft");
        record.NewState.ShouldBe("Published");
        record.TransitionedBy.ShouldBe("alice");
    }

    [Fact]
    public async Task OnTransitionedAsync_CapturesComment()
    {
        string db = NewDb();
        WorkflowTemplateTransitionHook<TestWorkflowDbContext> hook = CreateHook(db);

        using (WorkflowTransitionContext.SetComment("Validé par le directeur médical"))
        {
            await hook.OnTransitionedAsync(
                Guid.NewGuid(), TemplateLifecycleStatus.Draft, TemplateLifecycleStatus.Published, "alice",
                TestContext.Current.CancellationToken);
        }

        await using TestWorkflowDbContext ctx = new TestContextFactory(db).CreateDbContext();
        WorkflowTransitionRecord? record = await ctx.WorkflowTransitionRecords.FirstOrDefaultAsync(
            TestContext.Current.CancellationToken);

        record.ShouldNotBeNull();
        record.Comment.ShouldBe("Validé par le directeur médical");
    }

    [Fact]
    public async Task OnTransitionedAsync_IncludesTenantId_WhenAvailable()
    {
        string db = NewDb();
        Guid tenantId = Guid.NewGuid();
        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.IsAvailable.Returns(true);
        tenant.Id.Returns(tenantId);

        WorkflowTemplateTransitionHook<TestWorkflowDbContext> hook = CreateHook(db, currentTenant: tenant);

        await hook.OnTransitionedAsync(
            Guid.NewGuid(), TemplateLifecycleStatus.Draft, TemplateLifecycleStatus.Published, "alice",
            TestContext.Current.CancellationToken);

        await using TestWorkflowDbContext ctx = new TestContextFactory(db).CreateDbContext();
        WorkflowTransitionRecord? record = await ctx.WorkflowTransitionRecords.FirstOrDefaultAsync(
            TestContext.Current.CancellationToken);

        record.ShouldNotBeNull();
        record.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task OnTransitionedAsync_NullTenantId_WhenNotAvailable()
    {
        string db = NewDb();
        WorkflowTemplateTransitionHook<TestWorkflowDbContext> hook = CreateHook(db);

        await hook.OnTransitionedAsync(
            Guid.NewGuid(), TemplateLifecycleStatus.Draft, TemplateLifecycleStatus.Published, "alice",
            TestContext.Current.CancellationToken);

        await using TestWorkflowDbContext ctx = new TestContextFactory(db).CreateDbContext();
        WorkflowTransitionRecord? record = await ctx.WorkflowTransitionRecords.FirstOrDefaultAsync(
            TestContext.Current.CancellationToken);

        record.ShouldNotBeNull();
        record.TenantId.ShouldBeNull();
    }
}
