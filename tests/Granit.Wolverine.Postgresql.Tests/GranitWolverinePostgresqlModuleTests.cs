// =============================================================================
// Tests - GranitWolverinePostgresqlModule
// =============================================================================
// Verifies module inheritance, DependsOn declarations, and that
// AddGranitWolverineWithPostgresql() registers Wolverine services without throwing.
// =============================================================================

using FluentAssertions;
using Granit.Core.Modularity;
using Granit.Persistence;
using Granit.Wolverine.Postgresql.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Granit.Wolverine.Postgresql.Tests;

public sealed class GranitWolverinePostgresqlModuleTests
{
    [Fact]
    public void GranitWolverinePostgresqlModule_IsGranitModule() =>
        typeof(GranitWolverinePostgresqlModule).Should().BeAssignableTo<GranitModule>();

    [Fact]
    public void GranitWolverinePostgresqlModule_IsSealed() =>
        typeof(GranitWolverinePostgresqlModule).IsSealed.Should().BeTrue();

    [Fact]
    public void GranitWolverinePostgresqlModule_DependsOn_WolverineModule()
    {
        DependsOnAttribute[] attributes = (DependsOnAttribute[])
            typeof(GranitWolverinePostgresqlModule).GetCustomAttributes(typeof(DependsOnAttribute), inherit: false);

        attributes.Should().ContainSingle(a => a.DependedTypes.Contains(typeof(GranitWolverineModule)));
    }

    [Fact]
    public void GranitWolverinePostgresqlModule_DependsOn_PersistenceModule()
    {
        DependsOnAttribute[] attributes = (DependsOnAttribute[])
            typeof(GranitWolverinePostgresqlModule).GetCustomAttributes(typeof(DependsOnAttribute), inherit: false);

        attributes.Should().ContainSingle(a => a.DependedTypes.Contains(typeof(GranitPersistenceModule)));
    }

    [Fact]
    public void AddGranitWolverineWithPostgresql_RegistersWolverineServices()
    {
        IHostApplicationBuilder builder = Host.CreateApplicationBuilder();

        Action act = () => builder.AddGranitWolverineWithPostgresql(
            "Host=localhost;Database=test;Username=test;Password=test");

        act.Should().NotThrow();
    }
}
