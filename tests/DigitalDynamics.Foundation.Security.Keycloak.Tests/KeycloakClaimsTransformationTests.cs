// =============================================================================
// Tests - KeycloakClaimsTransformation
// =============================================================================
// Vérifie que les rôles Keycloak sont correctement mappés vers ClaimTypes.Role.
// Couvre les deux sources : realm_access (défaut) et resource_access.
// =============================================================================

using System.Security.Claims;
using DigitalDynamics.Foundation.Security.Keycloak.Authentication;
using FluentAssertions;
using Xunit;
using KeycloakOptions = DigitalDynamics.Foundation.Security.Keycloak.Options.KeycloakOptions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace DigitalDynamics.Foundation.Security.Keycloak.Tests;

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
        ClaimsIdentity identity = new ClaimsIdentity(
            [
                new Claim("sub", "user-123"),
                new Claim("realm_access", realmAccess)
            ],
            "Bearer");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

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
        ClaimsIdentity identity = new ClaimsIdentity(
            [
                new Claim("sub", "user-123"),
                new Claim("resource_access", resourceAccess)
            ],
            "Bearer");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

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
        ClaimsIdentity identity = new ClaimsIdentity(
            [new Claim("sub", "user-123")],
            "Bearer");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await CreateSut().TransformAsync(principal);

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
        ClaimsIdentity identity = new ClaimsIdentity(
            [
                new Claim("sub", "user-123"),
                new Claim("realm_access", realmAccess)
            ],
            "Bearer");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

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
        ClaimsIdentity identity = new ClaimsIdentity(
            [
                new Claim("sub", "user-123"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("realm_access", realmAccess)
            ],
            "Bearer");
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);

        // Act
        ClaimsPrincipal result = await CreateSut().TransformAsync(principal);

        // Assert
        result.FindAll(ClaimTypes.Role).Should().HaveCount(1);
    }
}
