// =============================================================================
// Tests - CurrentUserService
// =============================================================================
// Verifies the extraction of user information from JWT claims.
// UserName uses User.Identity.Name, which respects the NameClaimType
// configured in JWT Bearer (depends on the IDP provider used).
// =============================================================================

using System.Security.Claims;
using FluentAssertions;
using Granit.Authentication.JwtBearer.Authentication;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace Granit.Authentication.JwtBearer.Tests;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void UserId_WithAuthenticatedUser_ReturnsSubClaim()
    {
        // Arrange
        CurrentUserService sut = CreateService(new Claim("sub", "user-abc-123"));

        // Act & Assert
        sut.UserId.Should().Be("user-abc-123");
        sut.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void UserName_ReturnsIdentityName_ReflectsConfiguredNameClaimType()
    {
        // Arrange — nameType = "preferred_username" simulates the Keycloak config
        CurrentUserService sut = CreateService(
            nameType: "preferred_username",
            new Claim("sub", "user-123"),
            new Claim("preferred_username", "jean.dupont"));

        // Act & Assert — User.Identity.Name resolves the "preferred_username" claim
        sut.UserName.Should().Be("jean.dupont");
    }

    [Fact]
    public void UserName_WithGenericSubClaim_ReturnsSubValue()
    {
        // Arrange — nameType = "sub" (default JwtBearerAuthOptions.NameClaimType)
        CurrentUserService sut = CreateService(
            nameType: "sub",
            new Claim("sub", "user-abc-123"));

        // Act & Assert — User.Identity.Name resolves the "sub" claim
        sut.UserName.Should().Be("user-abc-123");
    }

    [Fact]
    public void Email_WithEmailClaim_ReturnsEmail()
    {
        // Arrange
        CurrentUserService sut = CreateService(
            new Claim("sub", "user-123"),
            new Claim(ClaimTypes.Email, "jean@guava-health.com"));

        // Act & Assert
        sut.Email.Should().Be("jean@guava-health.com");
    }

    [Fact]
    public void Roles_WithMultipleRoleClaims_ReturnsAllRoles()
    {
        // Arrange
        CurrentUserService sut = CreateService(
            new Claim("sub", "user-123"),
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "practitioner"));

        // Act & Assert
        sut.GetRoles().Should().BeEquivalentTo(["admin", "practitioner"]);
    }

    [Fact]
    public void IsInRole_WithMatchingRole_ReturnsTrue()
    {
        // Arrange
        CurrentUserService sut = CreateService(
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
        IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var sut = new CurrentUserService(accessor);

        // Act & Assert
        sut.UserId.Should().BeNull();
        sut.UserName.Should().BeNull();
        sut.Email.Should().BeNull();
        sut.IsAuthenticated.Should().BeFalse();
        sut.GetRoles().Should().BeEmpty();
    }

    // --- Helpers ---

    private static CurrentUserService CreateService(params Claim[] claims) =>
        CreateService(nameType: ClaimTypes.Name, claims);

    private static CurrentUserService CreateService(string nameType, params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Bearer", nameType, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        return new CurrentUserService(accessor);
    }
}
