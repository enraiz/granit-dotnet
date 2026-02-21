// =============================================================================
// Tests - KeycloakClaimsTransformation
// =============================================================================
// Vérifie que les rôles Keycloak (realm_access.roles) sont correctement
// mappés vers des ClaimTypes.Role standard .NET.
// =============================================================================

using System.Security.Claims;
using DigitalDynamics.Foundation.Security.Authentication;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Security.Tests;

public sealed class KeycloakClaimsTransformationTests
{
    private readonly KeycloakClaimsTransformation _sut = new();

    [Fact]
    public async Task TransformAsync_WithRealmAccessRoles_AddsRoleClaims()
    {
        // Arrange
        string realmAccess = """{"roles":["admin","practitioner"]}""";
        ClaimsIdentity identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "user-123"),
                new Claim("realm_access", realmAccess)
            },
            "Bearer");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await _sut.TransformAsync(principal);

        // Assert
        result.IsInRole("admin").Should().BeTrue();
        result.IsInRole("practitioner").Should().BeTrue();
        result.FindAll(ClaimTypes.Role).Should().HaveCount(2);
    }

    [Fact]
    public async Task TransformAsync_WithoutRealmAccess_ReturnsUnmodifiedPrincipal()
    {
        // Arrange
        ClaimsIdentity identity = new ClaimsIdentity(
            new[] { new Claim("sub", "user-123") },
            "Bearer");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await _sut.TransformAsync(principal);

        // Assert
        result.FindAll(ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_WithUnauthenticatedPrincipal_ReturnsUnmodifiedPrincipal()
    {
        // Arrange — pas de AuthenticationType → IsAuthenticated = false
        ClaimsIdentity identity = new ClaimsIdentity();
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await _sut.TransformAsync(principal);

        // Assert
        result.Identity!.IsAuthenticated.Should().BeFalse();
        result.FindAll(ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_WithEmptyRoles_AddsNoClaims()
    {
        // Arrange
        string realmAccess = """{"roles":[]}""";
        ClaimsIdentity identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "user-123"),
                new Claim("realm_access", realmAccess)
            },
            "Bearer");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await _sut.TransformAsync(principal);

        // Assert
        result.FindAll(ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_DoesNotDuplicateExistingRoles()
    {
        // Arrange
        string realmAccess = """{"roles":["admin"]}""";
        ClaimsIdentity identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "user-123"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("realm_access", realmAccess)
            },
            "Bearer");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await _sut.TransformAsync(principal);

        // Assert
        result.FindAll(ClaimTypes.Role).Should().HaveCount(1);
    }
}
