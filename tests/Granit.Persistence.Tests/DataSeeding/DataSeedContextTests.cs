// =============================================================================
// Tests — DataSeedContext
// =============================================================================
// Vérifie que le contexte de seeding :
//   - Stocke correctement le TenantId (null ou valeur)
//   - Gère le dictionnaire Properties via l'indexeur
//   - Retourne null pour une clé inexistante
// =============================================================================

using FluentAssertions;
using Granit.Persistence.DataSeeding;
using Xunit;

namespace Granit.Persistence.Tests.DataSeeding;

public sealed class DataSeedContextTests
{
    [Fact]
    public void Constructor_WithNoTenantId_TenantIdIsNull()
    {
        // Act
        DataSeedContext context = new();

        // Assert
        context.TenantId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithTenantId_StoresTenantId()
    {
        // Arrange
        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        DataSeedContext context = new(tenantId);

        // Assert
        context.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Properties_IsEmptyByDefault()
    {
        // Act
        DataSeedContext context = new();

        // Assert
        context.Properties.Should().BeEmpty();
    }

    [Fact]
    public void Properties_SetAndGet_ReturnsValue()
    {
        // Arrange
        DataSeedContext context = new();

        // Act
        context.Properties["AdminEmail"] = "admin@example.com";

        // Assert
        context.Properties["AdminEmail"].Should().Be("admin@example.com");
    }

    [Fact]
    public void Indexer_SetAndGet_ReturnsValue()
    {
        // Arrange
        DataSeedContext context = new();

        // Act
        context["Key"] = 42;

        // Assert
        context["Key"].Should().Be(42);
    }

    [Fact]
    public void Indexer_UnknownKey_ReturnsNull()
    {
        // Arrange
        DataSeedContext context = new();

        // Act
        object? value = context["NonExistent"];

        // Assert
        value.Should().BeNull();
    }
}
