// =============================================================================
// Tests - NullPermissionGrantStore
// =============================================================================
// Vérifie que l'implémentation par défaut (no-op) refuse toujours toute
// permission, quelle que soit la combinaison rôle / permission / tenant.
// =============================================================================

using FluentAssertions;
using Granit.Authorization.Services;
using Xunit;

namespace Granit.Authorization.Tests;

public sealed class NullPermissionGrantStoreTests
{
    [Fact]
    public async Task IsGrantedAsync_AlwaysReturnsFalse()
    {
        // Arrange
        NullPermissionGrantStore store = new();

        // Act
        bool result = await store.IsGrantedAsync(
            "admin",
            "Invoices.Delete",
            tenantId: null,
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse("NullPermissionGrantStore denies every permission by design");
    }

    [Theory]
    [InlineData("admin", "Invoices.Read", null)]
    [InlineData("editor", "Products.Create", "00000000-0000-0000-0000-000000000001")]
    [InlineData("viewer", "Reports.Export", "00000000-0000-0000-0000-000000000002")]
    public async Task IsGrantedAsync_AnyArguments_AlwaysReturnsFalse(
        string roleName,
        string permissionName,
        string? tenantIdString)
    {
        // Arrange
        NullPermissionGrantStore store = new();
        Guid? tenantId = tenantIdString is null ? null : Guid.Parse(tenantIdString);

        // Act
        bool result = await store.IsGrantedAsync(
            roleName,
            permissionName,
            tenantId,
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }
}
