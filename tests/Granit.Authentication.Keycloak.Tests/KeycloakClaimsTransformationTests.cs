// =============================================================================
// Tests - KeycloakClaimsTransformation
// =============================================================================
// Verifies that Keycloak roles are correctly mapped to ClaimTypes.Role.
// Covers both sources: realm_access (default) and resource_access.
// =============================================================================

using System.Security.Claims;
using FluentAssertions;
using Granit.Authentication.Keycloak.Authentication;
using Xunit;
using KeycloakOptions = Granit.Authentication.Keycloak.Options.KeycloakOptions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Granit.Authentication.Keycloak.Tests;

public sealed class KeycloakClaimsTransformationTests
{
    private static KeycloakClaimsTransformation CreateSut(
        string roleClaimsSource = "realm_access",
        string clientId = "test-client") =>
        new(OptionsFactory.Create(new KeycloakOptions
        {
            RoleClaimsSource = roleClaimsSource,
            ClientId = clientId
        }));

    [Fact]
    public async Task TransformAsync_WithRealmAccessRoles_AddsRoleClaims()
    {
        // Arrange
        string realmAccess = """{"roles":["admin","practitioner"]}""";
        var identity = new ClaimsIdentity(
            [
                new Claim("sub", "user-123"),
                new Claim("realm_access", realmAccess)
            ],
            "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await CreateSut().TransformAsync(principal);

        // Assert
        result.IsInRole("admin").Should().BeTrue();
        result.IsInRole("practitioner").Should().BeTrue();
        result.FindAll(ClaimTypes.Role).Should().HaveCount(2);
    }

    [Fact]
    public async Task TransformAsync_WithResourceAccessRoles_AddsRoleClaims()
    {
        // Arrange
        string resourceAccess = """{"test-client":{"roles":["admin"]}}""";
        var identity = new ClaimsIdentity(
            [
                new Claim("sub", "user-123"),
                new Claim("resource_access", resourceAccess)
            ],
            "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await CreateSut(roleClaimsSource: "resource_access").TransformAsync(principal);

        // Assert
        result.IsInRole("admin").Should().BeTrue();
        result.FindAll(ClaimTypes.Role).Should().HaveCount(1);
    }

    [Fact]
    public async Task TransformAsync_WithoutRealmAccess_ReturnsUnmodifiedPrincipal()
    {
        // Arrange
        var identity = new ClaimsIdentity(
            [new Claim("sub", "user-123")],
            "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await CreateSut().TransformAsync(principal);

        // Assert
        result.FindAll(ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_WithUnauthenticatedPrincipal_ReturnsUnmodifiedPrincipal()
    {
        // Arrange — no AuthenticationType → IsAuthenticated = false
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await CreateSut().TransformAsync(principal);

        // Assert
        result.Identity!.IsAuthenticated.Should().BeFalse();
        result.FindAll(ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_WithEmptyRoles_AddsNoClaims()
    {
        // Arrange
        string realmAccess = """{"roles":[]}""";
        var identity = new ClaimsIdentity(
            [
                new Claim("sub", "user-123"),
                new Claim("realm_access", realmAccess)
            ],
            "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await CreateSut().TransformAsync(principal);

        // Assert
        result.FindAll(ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_DoesNotDuplicateExistingRoles()
    {
        // Arrange
        string realmAccess = """{"roles":["admin"]}""";
        var identity = new ClaimsIdentity(
            [
                new Claim("sub", "user-123"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("realm_access", realmAccess)
            ],
            "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await CreateSut().TransformAsync(principal);

        // Assert
        result.FindAll(ClaimTypes.Role).Should().HaveCount(1);
    }
}
