// =============================================================================
// Tests - AddGranitWolverineWithPostgresqlPerTenant<TContext>
// =============================================================================
// Verifies that the extension method registers the per-tenant factory and DbContext
// as Scoped, and that the extension method itself does not throw during registration.
// DI registrations are inspected directly on IServiceCollection — the host is not
// built to avoid requiring a live PostgreSQL Outbox connection.
// =============================================================================

using FluentAssertions;
using Granit.Wolverine.Postgresql.Extensions;
using Granit.Wolverine.Postgresql.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Granit.Wolverine.Postgresql.Tests;

public sealed class AddGranitWolverineWithPostgresqlPerTenantTests
{
    private const string ValidTransportConnStr =
        "Host=localhost;Database=wolverine_transport;Username=test;Password=test";

    private static HostApplicationBuilder CreateBuilder()
    {
        HostApplicationBuilderSettings settings = new()
        {
            Configuration = new ConfigurationManager(),
        };
        settings.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{WolverinePostgresqlOptions.SectionName}:TransportConnectionString"] =
                ValidTransportConnStr,
        });
        return Host.CreateApplicationBuilder(settings);
    }

    // -----------------------------------------------------------------------
    // Registration — no throw
    // -----------------------------------------------------------------------

    [Fact]
    public void AddGranitWolverineWithPostgresqlPerTenant_DoesNotThrow()
    {
        HostApplicationBuilder builder = CreateBuilder();

        Action act = () => builder.AddGranitWolverineWithPostgresqlPerTenant<StubTenantDbContext>();

        act.Should().NotThrow();
    }

    // -----------------------------------------------------------------------
    // Registration — IDbContextFactory<TContext>
    // -----------------------------------------------------------------------

    [Fact]
    public void AddGranitWolverineWithPostgresqlPerTenant_RegistersDbContextFactory_AsScoped()
    {
        HostApplicationBuilder builder = CreateBuilder();
        builder.AddGranitWolverineWithPostgresqlPerTenant<StubTenantDbContext>();

        ServiceDescriptor? descriptor = builder.Services.FirstOrDefault(
            s => s.ServiceType == typeof(IDbContextFactory<StubTenantDbContext>));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitWolverineWithPostgresqlPerTenant_RegistersPerTenantDbContextFactory()
    {
        HostApplicationBuilder builder = CreateBuilder();
        builder.AddGranitWolverineWithPostgresqlPerTenant<StubTenantDbContext>();

        ServiceDescriptor? descriptor = builder.Services.FirstOrDefault(
            s => s.ServiceType == typeof(IDbContextFactory<StubTenantDbContext>));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should()
            .Be<PerTenantDbContextFactory<StubTenantDbContext>>();
    }

    // -----------------------------------------------------------------------
    // Registration — TContext
    // -----------------------------------------------------------------------

    [Fact]
    public void AddGranitWolverineWithPostgresqlPerTenant_RegistersDbContext_AsScoped()
    {
        HostApplicationBuilder builder = CreateBuilder();
        builder.AddGranitWolverineWithPostgresqlPerTenant<StubTenantDbContext>();

        ServiceDescriptor? descriptor = builder.Services.FirstOrDefault(
            s => s.ServiceType == typeof(StubTenantDbContext));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    // -----------------------------------------------------------------------
    // Registration — TryAdd preserves an existing factory registration
    // -----------------------------------------------------------------------

    [Fact]
    public void AddGranitWolverineWithPostgresqlPerTenant_PreservesExistingFactory_WhenAlreadyRegistered()
    {
        HostApplicationBuilder builder = CreateBuilder();

        // Pre-register a custom factory (e.g., from integration test setup) via lambda.
        builder.Services.AddScoped<IDbContextFactory<StubTenantDbContext>>(
            static _ => null!);

        builder.AddGranitWolverineWithPostgresqlPerTenant<StubTenantDbContext>();

        // TryAdd must not have replaced the existing lambda registration.
        ServiceDescriptor? descriptor = builder.Services.FirstOrDefault(
            s => s.ServiceType == typeof(IDbContextFactory<StubTenantDbContext>));

        // Lambda registration has no ImplementationType (it's null).
        descriptor!.ImplementationType.Should().BeNull();
    }
}
