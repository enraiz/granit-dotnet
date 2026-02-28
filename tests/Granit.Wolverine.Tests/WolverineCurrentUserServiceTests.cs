// =============================================================================
// Tests - WolverineCurrentUserService
// =============================================================================
// Verifies that the AsyncLocal override takes precedence over HttpContext,
// that Change() creates a restoring scope, and that nested scopes unwind
// correctly. Also covers the IHttpContextAccessor HTTP-context fallback path.
// =============================================================================

using System.Security.Claims;
using Granit.Wolverine.Internal;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Wolverine.Tests;

public sealed class WolverineCurrentUserServiceTests
{
    private static WolverineCurrentUserService CreateWithoutHttpContext()
    {
        IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        return new WolverineCurrentUserService(accessor);
    }

    private static WolverineCurrentUserService CreateWithHttpContext(
        bool isAuthenticated, params Claim[] claims)
    {
        ClaimsIdentity identity = new(claims, isAuthenticated ? "test" : null);
        ClaimsPrincipal principal = new(identity);
        DefaultHttpContext httpContext = new() { User = principal };
        IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return new WolverineCurrentUserService(accessor);
    }

    // -------------------------------------------------------------------------
    // UserId — no HTTP context
    // -------------------------------------------------------------------------

    [Fact]
    public void UserId_WithNoOverrideAndNoHttpContext_ReturnsNull()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        sut.UserId.ShouldBeNull();
    }

    [Fact]
    public void IsAuthenticated_WithNoOverrideAndNoHttpContext_ReturnsFalse()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        sut.IsAuthenticated.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // Change — AsyncLocal override
    // -------------------------------------------------------------------------

    [Fact]
    public void UserId_AfterChange_ReturnsOverrideValue()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();
        const string expectedUserId = "wolverine-user";

        using IDisposable scope = sut.Change(expectedUserId);

        sut.UserId.ShouldBe(expectedUserId);
    }

    [Fact]
    public void IsAuthenticated_AfterChange_ReturnsTrue()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        using IDisposable scope = sut.Change("any-user");

        sut.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public void UserId_AfterScopeDisposed_RestoresNull()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        IDisposable scope = sut.Change("temp-user");
        scope.Dispose();

        sut.UserId.ShouldBeNull();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();
        IDisposable scope = sut.Change("user");

        scope.Dispose();
        Action act = scope.Dispose;

        Should.NotThrow(act);
    }

    [Fact]
    public void NestedScopes_RestoreCorrectly()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();
        const string outer = "outer-user";
        const string inner = "inner-user";

        IDisposable outerScope = sut.Change(outer);
        sut.UserId.ShouldBe(outer);

        IDisposable innerScope = sut.Change(inner);
        sut.UserId.ShouldBe(inner);

        innerScope.Dispose();
        sut.UserId.ShouldBe(outer);

        outerScope.Dispose();
        sut.UserId.ShouldBeNull();
    }

    // -------------------------------------------------------------------------
    // Roles and other properties under override
    // -------------------------------------------------------------------------

    [Fact]
    public void Roles_WithOverrideActive_ReturnsEmptyList()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        using IDisposable scope = sut.Change("user");

        sut.GetRoles().ShouldBeEmpty();
    }

    [Fact]
    public void IsInRole_WithOverrideActive_ReturnsFalse()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        using IDisposable scope = sut.Change("user");

        sut.IsInRole("admin").ShouldBeFalse();
    }

    [Fact]
    public void UserName_WithOverrideActive_ReturnsNull()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        using IDisposable scope = sut.Change("user");

        sut.UserName.ShouldBeNull();
    }

    [Fact]
    public void Email_WithOverrideActive_ReturnsNull()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        using IDisposable scope = sut.Change("user");

        sut.Email.ShouldBeNull();
    }

    // -------------------------------------------------------------------------
    // HTTP context fallback path — no AsyncLocal override
    // -------------------------------------------------------------------------

    [Fact]
    public void UserId_WithSubClaim_ReturnsSubValue()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(
            isAuthenticated: true,
            new Claim("sub", "sub-user-123"));

        sut.UserId.ShouldBe("sub-user-123");
    }

    [Fact]
    public void UserId_WithNameIdentifierClaim_ReturnsNameIdentifier()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(
            isAuthenticated: true,
            new Claim(ClaimTypes.NameIdentifier, "ni-user-456"));

        sut.UserId.ShouldBe("ni-user-456");
    }

    [Fact]
    public void UserId_WithNoRelevantClaim_ReturnsNull()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(
            isAuthenticated: true,
            new Claim(ClaimTypes.Email, "user@example.com"));

        sut.UserId.ShouldBeNull();
    }

    [Fact]
    public void IsAuthenticated_WithAuthenticatedHttpContext_ReturnsTrue()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(isAuthenticated: true);

        sut.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithUnauthenticatedHttpContext_ReturnsFalse()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(isAuthenticated: false);

        sut.IsAuthenticated.ShouldBeFalse();
    }

    [Fact]
    public void UserName_WithHttpContext_ReturnsIdentityName()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(
            isAuthenticated: true,
            new Claim(ClaimTypes.Name, "jean.dupont"));

        sut.UserName.ShouldBe("jean.dupont");
    }

    [Fact]
    public void Email_WithClaimTypesEmail_ReturnsEmail()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(
            isAuthenticated: true,
            new Claim(ClaimTypes.Email, "user@example.com"));

        sut.Email.ShouldBe("user@example.com");
    }

    [Fact]
    public void Email_WithEmailClaim_ReturnsEmail()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(
            isAuthenticated: true,
            new Claim("email", "shorthand@example.com"));

        sut.Email.ShouldBe("shorthand@example.com");
    }

    [Fact]
    public void Roles_WithMultipleRoleClaims_ReturnsAll()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(
            isAuthenticated: true,
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "editor"));

        sut.GetRoles().ShouldBe(["admin", "editor"]);
    }

    [Fact]
    public void IsInRole_WithMatchingRole_ReturnsTrue()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(
            isAuthenticated: true,
            new Claim(ClaimTypes.Role, "admin"));

        sut.IsInRole("admin").ShouldBeTrue();
    }

    [Fact]
    public void IsInRole_WithNonMatchingRole_ReturnsFalse()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(
            isAuthenticated: true,
            new Claim(ClaimTypes.Role, "editor"));

        sut.IsInRole("admin").ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // Override takes precedence over HTTP context
    // -------------------------------------------------------------------------

    [Fact]
    public void UserId_WithOverrideAndHttpContext_IgnoresHttpContext()
    {
        WolverineCurrentUserService sut = CreateWithHttpContext(
            isAuthenticated: true,
            new Claim("sub", "http-user"));

        using IDisposable scope = sut.Change("override-user");

        sut.UserId.ShouldBe("override-user");
    }
}
