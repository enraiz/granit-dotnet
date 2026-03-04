// =============================================================================
// Tests - GranitNotificationsModule
// =============================================================================
// Verifies the module metadata: DependsOn attributes ensure the correct
// module dependency graph for Timing and Wolverine prerequisites.
// =============================================================================

using Granit.Core.Modularity;
using Granit.Timing;
using Granit.Wolverine;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class GranitNotificationsModuleTests
{
    [Fact]
    public void Module_DependsOnGranitTimingModule()
    {
        DependsOnAttribute[] attributes = typeof(GranitNotificationsModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), inherit: false)
            .Cast<DependsOnAttribute>()
            .ToArray();

        attributes.ShouldContain(a => a.DependedTypes.Contains(typeof(GranitTimingModule)));
    }

    [Fact]
    public void Module_DependsOnGranitWolverineModule()
    {
        DependsOnAttribute[] attributes = typeof(GranitNotificationsModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), inherit: false)
            .Cast<DependsOnAttribute>()
            .ToArray();

        attributes.ShouldContain(a => a.DependedTypes.Contains(typeof(GranitWolverineModule)));
    }

    [Fact]
    public void Module_IsGranitModule()
    {
        GranitNotificationsModule module = new();

        module.ShouldBeAssignableTo<GranitModule>();
    }
}
