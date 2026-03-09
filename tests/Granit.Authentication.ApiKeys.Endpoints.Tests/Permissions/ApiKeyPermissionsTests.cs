using Granit.Authentication.ApiKeys.Endpoints.Permissions;
using Shouldly;
using Xunit;

namespace Granit.Authentication.ApiKeys.Endpoints.Tests.Permissions;

public sealed class ApiKeyPermissionsTests
{
    [Fact]
    public void GroupName_Is_ApiKeys() =>
        ApiKeyPermissions.GroupName.ShouldBe("ApiKeys");

    [Theory]
    [InlineData("ApiKeys.Keys.Read")]
    [InlineData("ApiKeys.Keys.Create")]
    [InlineData("ApiKeys.Keys.Revoke")]
    [InlineData("ApiKeys.Keys.Rotate")]
    [InlineData("ApiKeys.Keys.UpdateScopes")]
    public void Permission_Constants_Follow_Convention(string permission) =>
        permission.ShouldStartWith(ApiKeyPermissions.GroupName);

    [Fact]
    public void Read_Permission_Value() =>
        ApiKeyPermissions.Keys.Read.ShouldBe("ApiKeys.Keys.Read");

    [Fact]
    public void Create_Permission_Value() =>
        ApiKeyPermissions.Keys.Create.ShouldBe("ApiKeys.Keys.Create");

    [Fact]
    public void Revoke_Permission_Value() =>
        ApiKeyPermissions.Keys.Revoke.ShouldBe("ApiKeys.Keys.Revoke");

    [Fact]
    public void Rotate_Permission_Value() =>
        ApiKeyPermissions.Keys.Rotate.ShouldBe("ApiKeys.Keys.Rotate");

    [Fact]
    public void UpdateScopes_Permission_Value() =>
        ApiKeyPermissions.Keys.UpdateScopes.ShouldBe("ApiKeys.Keys.UpdateScopes");
}
