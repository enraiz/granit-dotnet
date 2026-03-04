using Shouldly;
using Xunit;

namespace Granit.Authorization.Endpoints.Tests;

public sealed class AuthorizationEndpointsOptionsTests
{
    [Fact]
    public void ApiPrefix_Default_ShouldBeEmpty() =>
        new AuthorizationEndpointsOptions().ApiPrefix.ShouldBe(string.Empty);

    [Fact]
    public void RoutePrefix_Default_ShouldBeAuth() =>
        new AuthorizationEndpointsOptions().RoutePrefix.ShouldBe("auth");

    [Fact]
    public void TagName_Default_ShouldBeAuthorization() =>
        new AuthorizationEndpointsOptions().TagName.ShouldBe("Authorization");
}
