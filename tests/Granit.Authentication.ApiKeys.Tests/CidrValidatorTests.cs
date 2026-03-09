using System.Net;
using Shouldly;
using Xunit;

namespace Granit.Authentication.ApiKeys.Tests;

public sealed class CidrValidatorTests
{
    [Fact]
    public void IsAllowed_EmptyList_ReturnsTrue()
    {
        var ip = IPAddress.Parse("192.168.1.1");
        CidrValidator.IsAllowed(ip, []).ShouldBeTrue();
    }

    [Fact]
    public void IsAllowed_NullIpWithRestrictions_ReturnsFalse()
    {
        CidrValidator.IsAllowed(null, ["10.0.0.0/8"]).ShouldBeFalse();
    }

    [Fact]
    public void IsAllowed_IpInRange_ReturnsTrue()
    {
        var ip = IPAddress.Parse("10.1.2.3");
        CidrValidator.IsAllowed(ip, ["10.0.0.0/8"]).ShouldBeTrue();
    }

    [Fact]
    public void IsAllowed_IpOutOfRange_ReturnsFalse()
    {
        var ip = IPAddress.Parse("192.168.1.1");
        CidrValidator.IsAllowed(ip, ["10.0.0.0/8"]).ShouldBeFalse();
    }

    [Fact]
    public void IsAllowed_IpInSecondRange_ReturnsTrue()
    {
        var ip = IPAddress.Parse("192.168.1.1");
        CidrValidator.IsAllowed(ip, ["10.0.0.0/8", "192.168.0.0/16"]).ShouldBeTrue();
    }

    [Fact]
    public void IsAllowed_IPv6InRange_ReturnsTrue()
    {
        var ip = IPAddress.Parse("2001:db8::1");
        CidrValidator.IsAllowed(ip, ["2001:db8::/32"]).ShouldBeTrue();
    }

    [Fact]
    public void IsAllowed_IPv6OutOfRange_ReturnsFalse()
    {
        var ip = IPAddress.Parse("2001:db9::1");
        CidrValidator.IsAllowed(ip, ["2001:db8::/32"]).ShouldBeFalse();
    }

    [Fact]
    public void IsAllowed_ExactMatch_ReturnsTrue()
    {
        var ip = IPAddress.Parse("10.0.0.1");
        CidrValidator.IsAllowed(ip, ["10.0.0.1/32"]).ShouldBeTrue();
    }

    [Theory]
    [InlineData("10.0.0.0/8", true)]
    [InlineData("192.168.1.0/24", true)]
    [InlineData("2001:db8::/32", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData("10.0.0.0/32", true)]
    public void IsValidCidr_ValidatesCorrectly(string cidr, bool expected)
    {
        CidrValidator.IsValidCidr(cidr).ShouldBe(expected);
    }
}
