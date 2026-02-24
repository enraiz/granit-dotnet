// =============================================================================
// Tests - TenantPerSchemaDbContextFactory<TContext>
// =============================================================================
// Vérifie le guard tenant manquant et la construction correcte du DbContext.
// Aucune connexion PostgreSQL réelle n'est requise.
// =============================================================================

using FluentAssertions;
using Granit.Core.MultiTenancy;
using Granit.Persistence.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Granit.Persistence.Tests.MultiTenancy;

internal sealed class StubSchemaDbContext(DbContextOptions<StubSchemaDbContext> options)
    : DbContext(options);

public sealed class TenantPerSchemaDbContextFactoryTests
{
    private static readonly Guid TenantA = Guid.NewGuid();

    private static ICurrentTenant MakeTenant(Guid? id)
    {
        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(id);
        tenant.IsAvailable.Returns(id.HasValue);
        return tenant;
    }

    private static TenantPerSchemaDbContextFactory<StubSchemaDbContext> BuildFactory(
        ICurrentTenant currentTenant)
    {
        ITenantSchemaProvider schemaProvider = Substitute.For<ITenantSchemaProvider>();
#pragma warning disable CA2012 // NSubstitute setup pattern — ValueTask not consumed directly
        schemaProvider.GetSchemaNameAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("tenant_stub"));
#pragma warning restore CA2012

        ServiceCollection services = new();
        IServiceProvider sp = services.BuildServiceProvider();

        TenantPerSchemaDbContextOptions<StubSchemaDbContext> opts = new()
        {
            Configure = static builder => builder.UseInMemoryDatabase("schema_test"),
        };

        return new TenantPerSchemaDbContextFactory<StubSchemaDbContext>(
            currentTenant, schemaProvider, sp, opts);
    }

    // -----------------------------------------------------------------------
    // Happy path
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateDbContextAsync_WhenTenantActive_ReturnsDbContext()
    {
        TenantPerSchemaDbContextFactory<StubSchemaDbContext> factory =
            BuildFactory(MakeTenant(TenantA));

        await using StubSchemaDbContext ctx =
            await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        ctx.Should().NotBeNull();
    }

    [Fact]
    public void CreateDbContext_WhenTenantActive_ReturnsDbContext()
    {
        TenantPerSchemaDbContextFactory<StubSchemaDbContext> factory =
            BuildFactory(MakeTenant(TenantA));

        using StubSchemaDbContext ctx = factory.CreateDbContext();

        ctx.Should().NotBeNull();
    }

    // -----------------------------------------------------------------------
    // Guard — pas de tenant → exception (HDS : pas de fallback silencieux)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateDbContextAsync_WhenNoTenantActive_ThrowsInvalidOperationException()
    {
        TenantPerSchemaDbContextFactory<StubSchemaDbContext> factory =
            BuildFactory(MakeTenant(null));

        Func<Task> act = async () =>
            await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active tenant context*");
    }

    [Fact]
    public void CreateDbContext_WhenNoTenantActive_ThrowsInvalidOperationException()
    {
        TenantPerSchemaDbContextFactory<StubSchemaDbContext> factory =
            BuildFactory(MakeTenant(null));

        Action act = () => factory.CreateDbContext();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*No active tenant context*");
    }
}
