// =============================================================================
// Tests - GranitWolverineModule
// =============================================================================
// Verifies module inheritance, DependsOn declarations and that AddGranitWolverine()
// registers Wolverine services without throwing.
// =============================================================================

using FluentAssertions;
using Granit.Core.Modularity;
using Granit.MultiTenancy;
using Granit.Security;
using Granit.Wolverine.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Granit.Wolverine.Tests;

public sealed class GranitWolverineModuleTests
{
    [Fact]
    public void GranitWolverineModule_IsGranitModule() =>
        typeof(GranitWolverineModule).Should().BeAssignableTo<GranitModule>();

    [Fact]
    public void GranitWolverineModule_DependsOn_SecurityModule()
    {
        DependsOnAttribute[] attributes = (DependsOnAttribute[])
            typeof(GranitWolverineModule).GetCustomAttributes(typeof(DependsOnAttribute), inherit: false);

        attributes.Should().ContainSingle(a => a.DependedTypes.Contains(typeof(GranitSecurityModule)));
    }

    [Fact]
    public void GranitWolverineModule_DependsOn_MultiTenancyModule()
    {
        DependsOnAttribute[] attributes = (DependsOnAttribute[])
            typeof(GranitWolverineModule).GetCustomAttributes(typeof(DependsOnAttribute), inherit: false);

        attributes.Should().ContainSingle(a => a.DependedTypes.Contains(typeof(GranitMultiTenancyModule)));
    }

    [Fact]
    public void GranitWolverineModule_IsSealed() =>
        typeof(GranitWolverineModule).IsSealed.Should().BeTrue();

    [Fact]
    public void AddGranitWolverine_RegistersWolverineServices()
    {
        IHostApplicationBuilder builder = Host.CreateApplicationBuilder();

        Action act = () => builder.AddGranitWolverine();

        act.Should().NotThrow();
    }
}
