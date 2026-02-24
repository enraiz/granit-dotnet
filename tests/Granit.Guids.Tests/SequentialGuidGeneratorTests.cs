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

namespace Granit.Guids.Tests;

public sealed class SequentialGuidGeneratorTests
{
    private static SequentialGuidGenerator CreateGenerator(
        SequentialGuidType? guidType = null)
    {
        IOptions<GuidGeneratorOptions> options = Substitute.For<IOptions<GuidGeneratorOptions>>();
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
        SequentialGuidGenerator generator = CreateGenerator();

        // Act
        Guid guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_GeneratesUniqueGuids()
    {
        // Arrange
        SequentialGuidGenerator generator = CreateGenerator();
        HashSet<Guid> guids = [];

        // Act
        for (int i = 0; i < 10_000; i++)
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
        SequentialGuidGenerator generator = CreateGenerator(SequentialGuidType.SequentialAsString);
        List<string> guids = [];

        // Act - delai entre chaque batch pour garantir des timestamps differents
        for (int i = 0; i < 5; i++)
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
        SequentialGuidGenerator generator = CreateGenerator(SequentialGuidType.SequentialAtEnd);

        // Act
        Guid guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_SequentialAsBinary_GeneratesNonEmptyGuids()
    {
        // Arrange
        SequentialGuidGenerator generator = CreateGenerator(SequentialGuidType.SequentialAsBinary);

        // Act
        Guid guid = generator.Create();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Create_DefaultsToSequentialAsString()
    {
        // Arrange - pas de type specifie, le defaut doit etre SequentialAsString
        SequentialGuidGenerator generator = CreateGenerator();
        List<string> guids = [];

        // Act - delai entre chaque batch pour garantir des timestamps differents
        for (int i = 0; i < 5; i++)
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
        SequentialGuidGenerator generator = CreateGenerator(SequentialGuidType.SequentialAtEnd);
        List<Guid> guids = [];

        // Act
        for (int i = 0; i < 100; i++)
        {
            guids.Add(generator.Create());
        }

        // Assert - les 6 derniers octets doivent etre croissants (timestamp a la fin)
        var lastSixBytesList = guids
            .Select(g => g.ToByteArray()[10..16])
            .ToList();

        for (int i = 1; i < lastSixBytesList.Count; i++)
        {
            int comparison = CompareBytes(lastSixBytesList[i], lastSixBytesList[i - 1]);
            comparison.Should().BeGreaterThanOrEqualTo(0,
                "les 6 derniers octets (timestamp) doivent etre croissants pour SequentialAtEnd");
        }
    }

    private static int CompareBytes(byte[] a, byte[] b)
    {
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return a[i].CompareTo(b[i]);
            }
        }

        return 0;
    }
}
