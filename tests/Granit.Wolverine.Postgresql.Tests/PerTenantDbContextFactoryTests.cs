// =============================================================================
// Tests - PerTenantDbContextFactory<TContext>
// =============================================================================
// Verifies tenant routing, missing-tenant guard, and provider call correctness.
// No real PostgreSQL connection required: Npgsql DbContextOptions are created but
// never opened (connection only happens on query execution, not on DbContext construction).
// =============================================================================

using FluentAssertions;
using Granit.Core.MultiTenancy;
using Granit.Wolverine.Postgresql.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Granit.Wolverine.Postgresql.Tests;

// ---------------------------------------------------------------------------
// Minimal DbContext stub — single constructor (DbContextOptions<T>) required
// by PerTenantDbContextFactory<T>'s Activator.CreateInstance path.
// ---------------------------------------------------------------------------
internal sealed class StubTenantDbContext(DbContextOptions<StubTenantDbContext> options)
    : DbContext(options);

public sealed class PerTenantDbContextFactoryTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    private const string ConnA = "Host=host-a;Database=db_a;Username=usr;Password=pwd";
    private const string ConnB = "Host=host-b;Database=db_b;Username=usr;Password=pwd";

    // -----------------------------------------------------------------------
    // Helper — builds a factory with fully controlled dependencies.
    // -----------------------------------------------------------------------

    private static (PerTenantDbContextFactory<StubTenantDbContext> factory,
                    ITenantConnectionStringProvider provider)
        BuildFactoryWithProvider(Guid? tenantId, string connectionString)
    {
        ICurrentTenant currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(tenantId);

        ITenantConnectionStringProvider provider =
            Substitute.For<ITenantConnectionStringProvider>();
        provider
            .GetConnectionStringAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connectionString));

        ServiceCollection services = new();
        IServiceProvider sp = services.BuildServiceProvider();

        return (new PerTenantDbContextFactory<StubTenantDbContext>(
            currentTenant, provider, sp), provider);
    }

    private static PerTenantDbContextFactory<StubTenantDbContext> BuildFactory(
        Guid? tenantId, string connectionString) =>
        BuildFactoryWithProvider(tenantId, connectionString).factory;

    // -----------------------------------------------------------------------
    // CreateDbContextAsync — happy path
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateDbContextAsync_WhenTenantActive_ReturnsDbContext()
    {
        PerTenantDbContextFactory<StubTenantDbContext> factory = BuildFactory(TenantA, ConnA);

        await using StubTenantDbContext ctx =
            await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        ctx.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateDbContextAsync_WhenTenantActive_CallsProviderWithCorrectTenantId()
    {
        (PerTenantDbContextFactory<StubTenantDbContext> factory,
         ITenantConnectionStringProvider provider) = BuildFactoryWithProvider(TenantA, ConnA);

        await using StubTenantDbContext ctx =
            await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        // NSubstitute verification is synchronous; discard Task<string> return value.
        _ = provider.Received(1).GetConnectionStringAsync(TenantA, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDbContextAsync_TenantB_CallsProviderWithTenantBId()
    {
        (PerTenantDbContextFactory<StubTenantDbContext> factory,
         ITenantConnectionStringProvider provider) = BuildFactoryWithProvider(TenantB, ConnB);

        await using StubTenantDbContext ctx =
            await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        _ = provider.Received(1).GetConnectionStringAsync(TenantB, Arg.Any<CancellationToken>());
        _ = ctx;
    }

    // -----------------------------------------------------------------------
    // CreateDbContextAsync — missing tenant guard
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateDbContextAsync_WhenNoTenantActive_ThrowsInvalidOperationException()
    {
        PerTenantDbContextFactory<StubTenantDbContext> factory =
            BuildFactory(tenantId: null, connectionString: ConnA);

        Func<Task> act = async () =>
            await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active tenant context*");
    }

    [Fact]
    public async Task CreateDbContextAsync_WhenNoTenantActive_DoesNotCallProvider()
    {
        (PerTenantDbContextFactory<StubTenantDbContext> factory,
         ITenantConnectionStringProvider provider) = BuildFactoryWithProvider(
            tenantId: null, connectionString: ConnA);

        try
        {
            await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        }
        catch (InvalidOperationException) { }

        _ = provider.DidNotReceiveWithAnyArgs()
            .GetConnectionStringAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // CreateDbContext — synchronous overload
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateDbContext_WhenTenantActive_ReturnsDbContext()
    {
        PerTenantDbContextFactory<StubTenantDbContext> factory = BuildFactory(TenantA, ConnA);

        using StubTenantDbContext ctx = factory.CreateDbContext();

        ctx.Should().NotBeNull();
    }

    [Fact]
    public void CreateDbContext_WhenNoTenantActive_ThrowsInvalidOperationException()
    {
        PerTenantDbContextFactory<StubTenantDbContext> factory =
            BuildFactory(tenantId: null, connectionString: ConnA);

        Action act = () => factory.CreateDbContext();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*No active tenant context*");
    }
}
