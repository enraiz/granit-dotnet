// =============================================================================
// Tests - PermissionChecker
// =============================================================================
// Vérifie le pipeline RBAC complet :
//   1. AlwaysAllow → true sans store ni cache
//   2. Non authentifié → false
//   3. AdminRole bypass → true sans store ni cache
//   4. Permission inconnue → InvalidOperationException
//   5. Cache miss → store appelé, résultat mis en cache
//   6. Cache hit → store NON appelé
//   7. Logique OR multi-rôles
// =============================================================================

using FluentAssertions;
using Granit.Authorization.Abstractions;
using Granit.Authorization.Cache;
using Granit.Authorization.Options;
using Granit.Authorization.Services;
using Granit.Caching;
using Granit.MultiTenancy;
using Granit.Security;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Granit.Authorization.Tests;

public sealed class PermissionCheckerTests
{
    private const string DefinedPermission = "Invoices.Delete";
    private const string UndefinedPermission = "Unknown.Permission";
    private const string AdminRoleName = "admin";
    private static readonly Guid TenantId = Guid.NewGuid();

    // --- AlwaysAllow ---

    [Fact]
    public async Task IsGrantedAsync_AlwaysAllow_ReturnsTrueWithoutStoreOrCache()
    {
        // Arrange
        ICacheService<PermissionGrantCacheItem> cache = Substitute.For<ICacheService<PermissionGrantCacheItem>>();
        IPermissionGrantStore store = Substitute.For<IPermissionGrantStore>();
        PermissionChecker checker = BuildChecker(
            alwaysAllow: true,
            isAuthenticated: false,
            cache: cache,
            store: store);

        // Act
        bool result = await checker.IsGrantedAsync(DefinedPermission, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        await cache.DidNotReceive().GetOrAddAsync(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, Task<PermissionGrantCacheItem>>>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        await store.DidNotReceive().IsGrantedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    // --- Not authenticated ---

    [Fact]
    public async Task IsGrantedAsync_NotAuthenticated_ReturnsFalse()
    {
        // Arrange
        PermissionChecker checker = BuildChecker(isAuthenticated: false);

        // Act
        bool result = await checker.IsGrantedAsync(DefinedPermission, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    // --- AdminRole bypass ---

    [Fact]
    public async Task IsGrantedAsync_UserHasAdminRole_ReturnsTrueWithoutStoreOrCache()
    {
        // Arrange
        ICacheService<PermissionGrantCacheItem> cache = Substitute.For<ICacheService<PermissionGrantCacheItem>>();
        IPermissionGrantStore store = Substitute.For<IPermissionGrantStore>();
        PermissionChecker checker = BuildChecker(
            isAuthenticated: true,
            roles: [AdminRoleName],
            cache: cache,
            store: store);

        // Act
        bool result = await checker.IsGrantedAsync(DefinedPermission, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        await cache.DidNotReceive().GetOrAddAsync(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, Task<PermissionGrantCacheItem>>>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
        await store.DidNotReceive().IsGrantedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    // --- Undefined permission ---

    [Fact]
    public async Task IsGrantedAsync_UndefinedPermission_ThrowsInvalidOperationException()
    {
        // Arrange
        PermissionChecker checker = BuildChecker(isAuthenticated: true, roles: ["editor"]);

        // Act
        Func<Task> act = () => checker.IsGrantedAsync(UndefinedPermission);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*'{UndefinedPermission}'*");
    }

    // --- Cache miss → store called ---

    [Fact]
    public async Task IsGrantedAsync_CacheMiss_StoreCalledAndGrantCached()
    {
        // Arrange
        IPermissionGrantStore store = Substitute.For<IPermissionGrantStore>();
        store.IsGrantedAsync("editor", DefinedPermission, TenantId, Arg.Any<CancellationToken>())
            .Returns(true);

        ICacheService<PermissionGrantCacheItem> cache = BuildPassThroughCache();

        PermissionChecker checker = BuildChecker(
            isAuthenticated: true,
            roles: ["editor"],
            tenantId: TenantId,
            store: store,
            cache: cache);

        // Act
        bool result = await checker.IsGrantedAsync(DefinedPermission, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        await store.Received(1).IsGrantedAsync(
            "editor", DefinedPermission, TenantId, Arg.Any<CancellationToken>());
        await cache.Received(1).GetOrAddAsync(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, Task<PermissionGrantCacheItem>>>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    // --- Cache hit → store NOT called ---

    [Fact]
    public async Task IsGrantedAsync_CacheHit_StoreNotCalled()
    {
        // Arrange
        IPermissionGrantStore store = Substitute.For<IPermissionGrantStore>();

        ICacheService<PermissionGrantCacheItem> cache = Substitute.For<ICacheService<PermissionGrantCacheItem>>();
        cache.GetOrAddAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<PermissionGrantCacheItem>>>(),
                Arg.Any<DistributedCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new PermissionGrantCacheItem { IsGranted = true }); // cache hit — factory not invoked

        PermissionChecker checker = BuildChecker(
            isAuthenticated: true,
            roles: ["editor"],
            store: store,
            cache: cache);

        // Act
        bool result = await checker.IsGrantedAsync(DefinedPermission, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        await store.DidNotReceive().IsGrantedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    // --- Multi-role OR logic ---

    [Fact]
    public async Task IsGrantedAsync_SecondRoleHasGrant_ReturnsTrue()
    {
        // Arrange
        IPermissionGrantStore store = Substitute.For<IPermissionGrantStore>();
        store.IsGrantedAsync("reader", DefinedPermission, null, Arg.Any<CancellationToken>())
            .Returns(false);
        store.IsGrantedAsync("editor", DefinedPermission, null, Arg.Any<CancellationToken>())
            .Returns(true);

        PermissionChecker checker = BuildChecker(
            isAuthenticated: true,
            roles: ["reader", "editor"],
            store: store,
            cache: BuildPassThroughCache());

        // Act
        bool result = await checker.IsGrantedAsync(DefinedPermission, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGrantedAsync_NoRoleHasGrant_ReturnsFalse()
    {
        // Arrange
        IPermissionGrantStore store = Substitute.For<IPermissionGrantStore>();
        store.IsGrantedAsync(Arg.Any<string>(), DefinedPermission, null, Arg.Any<CancellationToken>())
            .Returns(false);

        PermissionChecker checker = BuildChecker(
            isAuthenticated: true,
            roles: ["reader", "editor"],
            store: store,
            cache: BuildPassThroughCache());

        // Act
        bool result = await checker.IsGrantedAsync(DefinedPermission, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    // --- BuildCacheKey ---

    [Fact]
    public void BuildCacheKey_WithTenantId_FormatsCorrectly()
    {
        Guid tenant = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        string key = PermissionChecker.BuildCacheKey(tenant, "editor", "Invoices.Delete");
        key.Should().Be($"perm:{tenant}:editor:Invoices.Delete");
    }

    [Fact]
    public void BuildCacheKey_WithoutTenantId_UsesGlobalSegment()
    {
        string key = PermissionChecker.BuildCacheKey(null, "editor", "Invoices.Delete");
        key.Should().Be("perm:global:editor:Invoices.Delete");
    }

    // --- Helpers ---

    private static PermissionChecker BuildChecker(
        bool alwaysAllow = false,
        bool isAuthenticated = true,
        string[]? roles = null,
        Guid? tenantId = null,
        IPermissionGrantStore? store = null,
        ICacheService<PermissionGrantCacheItem>? cache = null)
    {
        ICurrentUserService user = Substitute.For<ICurrentUserService>();
        user.IsAuthenticated.Returns(isAuthenticated);
        user.GetRoles().Returns((roles ?? []).ToList().AsReadOnly());
        user.IsInRole(Arg.Any<string>()).Returns(false);
        foreach (string role in roles ?? [])
        {
            user.IsInRole(role).Returns(true);
        }

        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(tenantId);

        IPermissionDefinitionManager manager = Substitute.For<IPermissionDefinitionManager>();
        manager.Exists(DefinedPermission).Returns(true);
        manager.Exists(UndefinedPermission).Returns(false);

        IPermissionGrantStore grantStore = store ?? Substitute.For<IPermissionGrantStore>();
        ICacheService<PermissionGrantCacheItem> cacheService = cache ?? BuildPassThroughCache();

        GranitAuthorizationOptions opts = new()
        {
            AdminRoles = [AdminRoleName],
            AlwaysAllow = alwaysAllow,
            CacheDuration = TimeSpan.FromMinutes(5)
        };

        return new PermissionChecker(user, tenant, manager, grantStore, cacheService, Microsoft.Extensions.Options.Options.Create(opts));
    }

    /// <summary>
    /// Cache substitute that always calls the factory (simulates a cache miss on every call).
    /// </summary>
    private static ICacheService<PermissionGrantCacheItem> BuildPassThroughCache()
    {
        ICacheService<PermissionGrantCacheItem> cache = Substitute.For<ICacheService<PermissionGrantCacheItem>>();
        cache.GetOrAddAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<PermissionGrantCacheItem>>>(),
                Arg.Any<DistributedCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                Func<CancellationToken, Task<PermissionGrantCacheItem>> factory =
                    callInfo.ArgAt<Func<CancellationToken, Task<PermissionGrantCacheItem>>>(1);
                return factory(CancellationToken.None);
            });
        return cache;
    }
}
