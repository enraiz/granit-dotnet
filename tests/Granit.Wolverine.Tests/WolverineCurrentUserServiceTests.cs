// =============================================================================
// Tests - WolverineCurrentUserService
// =============================================================================
// Verifies that the AsyncLocal override takes precedence over HttpContext,
// that Change() creates a restoring scope, and that nested scopes unwind
// correctly.
// =============================================================================

using FluentAssertions;
using Granit.Wolverine.Internal;
using Microsoft.AspNetCore.Http;
using NSubstitute;
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

    // -------------------------------------------------------------------------
    // UserId — no HTTP context
    // -------------------------------------------------------------------------

    [Fact]
    public void UserId_WithNoOverrideAndNoHttpContext_ReturnsNull()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        sut.UserId.Should().BeNull();
    }

    [Fact]
    public void IsAuthenticated_WithNoOverrideAndNoHttpContext_ReturnsFalse()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        sut.IsAuthenticated.Should().BeFalse();
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

        sut.UserId.Should().Be(expectedUserId);
    }

    [Fact]
    public void IsAuthenticated_AfterChange_ReturnsTrue()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        using IDisposable scope = sut.Change("any-user");

        sut.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void UserId_AfterScopeDisposed_RestoresNull()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        IDisposable scope = sut.Change("temp-user");
        scope.Dispose();

        sut.UserId.Should().BeNull();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();
        IDisposable scope = sut.Change("user");

        scope.Dispose();
        Action act = scope.Dispose;

        act.Should().NotThrow();
    }

    [Fact]
    public void NestedScopes_RestoreCorrectly()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();
        const string outer = "outer-user";
        const string inner = "inner-user";

        IDisposable outerScope = sut.Change(outer);
        sut.UserId.Should().Be(outer);

        IDisposable innerScope = sut.Change(inner);
        sut.UserId.Should().Be(inner);

        innerScope.Dispose();
        sut.UserId.Should().Be(outer);

        outerScope.Dispose();
        sut.UserId.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // Roles and other properties under override
    // -------------------------------------------------------------------------

    [Fact]
    public void Roles_WithOverrideActive_ReturnsEmptyList()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        using IDisposable scope = sut.Change("user");

        sut.Roles.Should().BeEmpty();
    }

    [Fact]
    public void IsInRole_WithOverrideActive_ReturnsFalse()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        using IDisposable scope = sut.Change("user");

        sut.IsInRole("admin").Should().BeFalse();
    }

    [Fact]
    public void UserName_WithOverrideActive_ReturnsNull()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        using IDisposable scope = sut.Change("user");

        sut.UserName.Should().BeNull();
    }

    [Fact]
    public void Email_WithOverrideActive_ReturnsNull()
    {
        WolverineCurrentUserService sut = CreateWithoutHttpContext();

        using IDisposable scope = sut.Change("user");

        sut.Email.Should().BeNull();
    }
}
