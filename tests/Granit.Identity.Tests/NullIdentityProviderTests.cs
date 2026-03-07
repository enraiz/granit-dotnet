using Granit.Identity.Internal;
using Granit.Identity.Models;
using Shouldly;
using Xunit;

namespace Granit.Identity.Tests;

public sealed class NullIdentityProviderTests
{
    private readonly NullIdentityProvider _provider = new();

    [Fact]
    public async Task GetUsersAsync_ReturnsEmptyList()
    {
        IReadOnlyList<IdentityUser> result = await _provider.GetUsersAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUsersAsync_WithSearch_ReturnsEmptyList()
    {
        IReadOnlyList<IdentityUser> result = await _provider.GetUsersAsync(
            search: "alice", first: 0, max: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUsersAsync_WithPagination_ReturnsEmptyList()
    {
        IReadOnlyList<IdentityUser> result = await _provider.GetUsersAsync(
            first: 5, max: 25,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUserAsync_ReturnsNull()
    {
        IdentityUser? result = await _provider.GetUserAsync(
            "any-user-id", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetUserAsync_WithEmptyId_ReturnsNull()
    {
        IdentityUser? result = await _provider.GetUserAsync(
            string.Empty, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetUserEnabledAsync_CompletesWithoutError()
    {
        await Should.NotThrowAsync(async () =>
        {
            await _provider.SetUserEnabledAsync("user-1", true, TestContext.Current.CancellationToken);
            await _provider.SetUserEnabledAsync("user-1", false, TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task GetUserSessionsAsync_ReturnsEmptyList()
    {
        IReadOnlyList<IdentitySession> result = await _provider.GetUserSessionsAsync(
            "any-user-id", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUserDeviceActivityAsync_ReturnsEmptyList()
    {
        IReadOnlyList<IdentityDeviceActivity> result = await _provider.GetUserDeviceActivityAsync(
            "any-user-id", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPasswordChangedAtAsync_ReturnsNull()
    {
        DateTimeOffset? result = await _provider.GetPasswordChangedAtAsync(
            "any-user-id", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetRolesAsync_ReturnsEmptyList()
    {
        IReadOnlyList<IdentityRole> result = await _provider.GetRolesAsync(
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRoleMembersAsync_ReturnsEmptyList()
    {
        IReadOnlyList<IdentityUser> result = await _provider.GetRoleMembersAsync(
            "any-role", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRoleMembersAsync_WithEmptyRole_ReturnsEmptyList()
    {
        IReadOnlyList<IdentityUser> result = await _provider.GetRoleMembersAsync(
            string.Empty, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateUserAsync_CompletesWithoutError()
    {
        await Should.NotThrowAsync(async () =>
        {
            IdentityUserUpdate update = new("new@test.com", "Alice", "Doe");
            await _provider.UpdateUserAsync("user-1", update, TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public void ImplementsIIdentityProvider() =>
        _provider.ShouldBeAssignableTo<IIdentityProvider>();
}
