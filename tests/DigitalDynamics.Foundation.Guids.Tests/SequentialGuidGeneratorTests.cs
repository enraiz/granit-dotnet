// =============================================================================
// Tests - SequentialGuidGenerator
// =============================================================================
// Verifie que l'implementation SequentialGuidGenerator :
//   - Genere des GUID non vides
//   - Genere des GUID uniques (10 000 iterations)
//   - Genere des GUID sequentiels (ordonnancement string croissant)
//   - Respecte la configuration du type sequentiel
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.Guids.Tests;

public sealed class SequentialGuidGeneratorTests
{
    private static SequentialGuidGenerator CreateGenerator(
        SequentialGuidType? guidType = null)
    {
        var options = Substitute.For<IOptions<GuidGeneratorOptions>>();
        options.Value.Returns(new GuidGeneratorOptions
        {
            DefaultSequentialGuidType = guidType
        });
        return new SequentialGuidGenerator(options);
    }

    [Fact]
    public void Create_ReturnsNonEmptyGuid()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_GeneratesUniqueGuids()
    {
        // Arrange
        var generator = CreateGenerator();
        var guids = new HashSet<Guid>();

        // Act
        for (var i = 0; i < 10_000; i++)
        {
            guids.Add(generator.Create());
        }

        // Assert
        guids.Should().HaveCount(10_000, "tous les GUID doivent etre uniques");
    }

    [Fact]
    public async Task Create_SequentialAsString_GeneratesOrderedGuids()
    {
        // Arrange
        var generator = CreateGenerator(SequentialGuidType.SequentialAsString);
        var guids = new List<string>();

        // Act - delai entre chaque batch pour garantir des timestamps differents
        for (var i = 0; i < 5; i++)
        {
            guids.Add(generator.Create().ToString());
            await Task.Delay(2, TestContext.Current.CancellationToken);
        }

        // Assert - les representations string doivent etre en ordre croissant
        guids.Should().BeInAscendingOrder(
            "les GUID SequentialAsString doivent etre ordonnes par string");
    }

    [Fact]
    public void Create_SequentialAtEnd_GeneratesNonEmptyGuids()
    {
        // Arrange
        var generator = CreateGenerator(SequentialGuidType.SequentialAtEnd);

        // Act
        var guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_SequentialAsBinary_GeneratesNonEmptyGuids()
    {
        // Arrange
        var generator = CreateGenerator(SequentialGuidType.SequentialAsBinary);

        // Act
        var guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Create_DefaultsToSequentialAsString()
    {
        // Arrange - pas de type specifie, le defaut doit etre SequentialAsString
        var generator = CreateGenerator();
        var guids = new List<string>();

        // Act - delai entre chaque batch pour garantir des timestamps differents
        for (var i = 0; i < 5; i++)
        {
            guids.Add(generator.Create().ToString());
            await Task.Delay(2, TestContext.Current.CancellationToken);
        }

        // Assert - si le defaut est SequentialAsString, les strings sont ordonnees
        guids.Should().BeInAscendingOrder(
            "le defaut doit etre SequentialAsString (PostgreSQL)");
    }

    [Fact]
    public void Create_WithExplicitType_UsesSpecifiedType()
    {
        // Arrange
        var generator = CreateGenerator(SequentialGuidType.SequentialAtEnd);
        var guids = new List<Guid>();

        // Act
        for (var i = 0; i < 100; i++)
        {
            guids.Add(generator.Create());
        }

        // Assert - les 6 derniers octets doivent etre croissants (timestamp a la fin)
        var lastSixBytesList = guids
            .Select(g => g.ToByteArray()[10..16])
            .ToList();

        for (var i = 1; i < lastSixBytesList.Count; i++)
        {
            var comparison = CompareBytes(lastSixBytesList[i], lastSixBytesList[i - 1]);
            comparison.Should().BeGreaterThanOrEqualTo(0,
                "les 6 derniers octets (timestamp) doivent etre croissants pour SequentialAtEnd");
        }
    }

    private static int CompareBytes(byte[] a, byte[] b)
    {
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return a[i].CompareTo(b[i]);
            }
        }

        return 0;
    }
}
