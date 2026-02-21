// =============================================================================
// Tests - CurrentUserService
// =============================================================================
// Vérifie l'extraction des informations utilisateur depuis les claims JWT.
// =============================================================================

using System.Security.Claims;
using DigitalDynamics.Foundation.Security.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.Security.Tests;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void UserId_WithAuthenticatedUser_ReturnsSubClaim()
    {
        // Arrange
        var sut = CreateService(new Claim("sub", "user-abc-123"));

        // Act & Assert
        sut.UserId.Should().Be("user-abc-123");
        sut.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void UserName_WithPreferredUsername_ReturnsPreferredUsername()
    {
        // Arrange
        var sut = CreateService(
            new Claim("sub", "user-123"),
            new Claim("preferred_username", "jean.dupont"));

        // Act & Assert
        sut.UserName.Should().Be("jean.dupont");
    }

    [Fact]
    public void Email_WithEmailClaim_ReturnsEmail()
    {
        // Arrange
        var sut = CreateService(
            new Claim("sub", "user-123"),
            new Claim(ClaimTypes.Email, "jean@guava-health.com"));

        // Act & Assert
        sut.Email.Should().Be("jean@guava-health.com");
    }

    [Fact]
    public void Roles_WithMultipleRoleClaims_ReturnsAllRoles()
    {
        // Arrange
        var sut = CreateService(
            new Claim("sub", "user-123"),
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "practitioner"));

        // Act & Assert
        sut.Roles.Should().BeEquivalentTo(new[] { "admin", "practitioner" });
    }

    [Fact]
    public void IsInRole_WithMatchingRole_ReturnsTrue()
    {
        // Arrange
        var sut = CreateService(
            new Claim("sub", "user-123"),
            new Claim(ClaimTypes.Role, "admin"));

        // Act & Assert
        sut.IsInRole("admin").Should().BeTrue();
        sut.IsInRole("unknown").Should().BeFalse();
    }

    [Fact]
    public void Properties_WithoutHttpContext_ReturnDefaults()
    {
        // Arrange
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var sut = new CurrentUserService(accessor);

        // Act & Assert
        sut.UserId.Should().BeNull();
        sut.UserName.Should().BeNull();
        sut.Email.Should().BeNull();
        sut.IsAuthenticated.Should().BeFalse();
        sut.Roles.Should().BeEmpty();
    }

    private static CurrentUserService CreateService(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        return new CurrentUserService(accessor);
    }
}
