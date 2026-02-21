// =============================================================================
// Tests - SimpleGuidGenerator
// =============================================================================
// Verifie que SimpleGuidGenerator :
//   - Genere des GUID non vides
//   - Genere des GUID uniques
//   - Fournit une instance statique
// =============================================================================

using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Guids.Tests;

public sealed class SimpleGuidGeneratorTests
{
    [Fact]
    public void Create_ReturnsNonEmptyGuid()
    {
        // Arrange
        var generator = new SimpleGuidGenerator();

        // Act
        var guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_GeneratesUniqueGuids()
    {
        // Arrange
        var generator = new SimpleGuidGenerator();

        // Act
        var guid1 = generator.Create();
        var guid2 = generator.Create();

        // Assert
        guid1.Should().NotBe(guid2);
    }

    [Fact]
    public void Instance_IsNotNull()
    {
        SimpleGuidGenerator.Instance.Should().NotBeNull();
    }

    [Fact]
    public void Instance_CreateReturnsNonEmptyGuid()
    {
        // Act
        var guid = SimpleGuidGenerator.Instance.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }
}
