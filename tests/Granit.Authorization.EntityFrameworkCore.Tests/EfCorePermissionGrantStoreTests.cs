// =============================================================================
// Tests - EfCorePermissionGrantStore
// =============================================================================
// Vérifie que le store :
//   - Retourne true pour un grant existant
//   - Retourne false si le rôle, la permission ou le tenant ne correspond pas
//   - Gère correctement les grants à portée globale (TenantId null)
// =============================================================================

using FluentAssertions;
using Granit.Authorization.EntityFrameworkCore.DbContext;
using Granit.Authorization.EntityFrameworkCore.Entities;
using Granit.Authorization.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Granit.Authorization.EntityFrameworkCore.Tests;

public sealed class EfCorePermissionGrantStoreTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    [Fact]
    public async Task IsGrantedAsync_MatchingGrant_ReturnsTrue()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        await SeedAsync(context, "accountant", "Invoices.Delete", TenantA);
        EfCorePermissionGrantStore<TestDbContext> store = new(context);

        // Act
        bool result = await store.IsGrantedAsync("accountant", "Invoices.Delete", TenantA, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGrantedAsync_DifferentRole_ReturnsFalse()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        await SeedAsync(context, "accountant", "Invoices.Delete", TenantA);
        EfCorePermissionGrantStore<TestDbContext> store = new(context);

        // Act
        bool result = await store.IsGrantedAsync("reader", "Invoices.Delete", TenantA, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_DifferentPermission_ReturnsFalse()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        await SeedAsync(context, "accountant", "Invoices.Delete", TenantA);
        EfCorePermissionGrantStore<TestDbContext> store = new(context);

        // Act
        bool result = await store.IsGrantedAsync("accountant", "Invoices.Read", TenantA, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_DifferentTenant_ReturnsFalse()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        await SeedAsync(context, "accountant", "Invoices.Delete", TenantA);
        EfCorePermissionGrantStore<TestDbContext> store = new(context);

        // Act
        bool result = await store.IsGrantedAsync("accountant", "Invoices.Delete", TenantB, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_GlobalGrant_NullTenantMatches()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        await SeedAsync(context, "admin", "System.Configure", tenantId: null);
        EfCorePermissionGrantStore<TestDbContext> store = new(context);

        // Act
        bool result = await store.IsGrantedAsync("admin", "System.Configure", tenantId: null, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGrantedAsync_GlobalGrantDoesNotMatchTenant_ReturnsFalse()
    {
        // Arrange
        await using TestDbContext context = CreateContext();
        await SeedAsync(context, "admin", "System.Configure", tenantId: null);
        EfCorePermissionGrantStore<TestDbContext> store = new(context);

        // Act — tenant-scoped query should not match the global (null) grant
        bool result = await store.IsGrantedAsync("admin", "System.Configure", TenantA, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    // --- Helpers ---

    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task SeedAsync(
        TestDbContext context,
        string roleName,
        string permissionName,
        Guid? tenantId)
    {
        context.PermissionGrants.Add(new PermissionGrant
        {
            Id = Guid.NewGuid(),
            Name = permissionName,
            RoleName = roleName,
            TenantId = tenantId
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

}
