using Granit.Authorization.Abstractions;
using Granit.Identity.Endpoints.Permissions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Identity.Endpoints.Tests;

/// <summary>
/// Tests that the identity permission definition provider registers all expected permissions.
/// </summary>
public sealed class IdentityPermissionDefinitionProviderTests
{
    [Fact]
    public void DefinePermissions_registers_all_permissions()
    {
        var context = Substitute.For<IPermissionDefinitionContext>();
        PermissionGroup group = new("Identity", "Identity");

        context.AddGroup("Identity", "Identity").Returns(group);

        var provider = new IdentityPermissionDefinitionProvider();
        provider.DefinePermissions(context);

        context.Received(1).AddGroup("Identity", "Identity");

        // Verify permissions were added to the real group
        group.Permissions.Count.ShouldBe(3);
        group.Permissions.ShouldContain(p => p.Name == IdentityUserCachePermissions.UserCache.Read);
        group.Permissions.ShouldContain(p => p.Name == IdentityUserCachePermissions.UserCache.Sync);
        group.Permissions.ShouldContain(p => p.Name == IdentityUserCachePermissions.UserCache.Delete);
    }
}
