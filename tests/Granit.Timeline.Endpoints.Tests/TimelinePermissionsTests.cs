using Granit.Timeline.Endpoints.Permissions;
using Shouldly;
using Xunit;

namespace Granit.Timeline.Endpoints.Tests;

/// <summary>
/// Unit tests verifying <see cref="TimelinePermissions"/> constants.
/// </summary>
public sealed class TimelinePermissionsTests
{
    [Fact]
    public void GroupName_is_Timeline() =>
        TimelinePermissions.GroupName.ShouldBe("Timeline");

    [Fact]
    public void Read_Default_is_Timeline_Read() =>
        TimelinePermissions.Read.Default.ShouldBe("Timeline.Read");

    [Fact]
    public void Write_Default_is_Timeline_Write() =>
        TimelinePermissions.Write.Default.ShouldBe("Timeline.Write");
}
