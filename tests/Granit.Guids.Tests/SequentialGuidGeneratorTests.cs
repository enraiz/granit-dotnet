// =============================================================================
// Tests - SequentialGuidGenerator
// =============================================================================
// Verifie que l'implementation SequentialGuidGenerator :
//   - Genere des GUID non vides
//   - Genere des GUID uniques (10 000 iterations)
//   - Genere des GUID sequentiels (ordonnancement string croissant)
//   - Respecte la configuration du type sequentiel
// =============================================================================

using Granit.Timing;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Guids.Tests;

public sealed class SequentialGuidGeneratorTests
{
    private static SequentialGuidGenerator CreateGenerator(
        SequentialGuidType? guidType = null,
        IClock? clock = null)
    {
        IOptions<GuidGeneratorOptions> options = Substitute.For<IOptions<GuidGeneratorOptions>>();
        options.Value.Returns(new GuidGeneratorOptions
        {
            DefaultSequentialGuidType = guidType
        });
        if (clock is null)
        {
            clock = Substitute.For<IClock>();
            clock.Now.Returns(_ => DateTimeOffset.UtcNow);
        }

        return new SequentialGuidGenerator(options, clock);
    }

    [Fact]
    public void Create_ReturnsNonEmptyGuid()
    {
        // Arrange
        SequentialGuidGenerator generator = CreateGenerator();

        // Act
        Guid guid = generator.Create();

        // Assert
        guid.ShouldNotBe(Guid.Empty);
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
        guids.Count.ShouldBe(10_000, "tous les GUID doivent etre uniques");
    }

    [Fact]
    public void Create_SequentialAsString_GeneratesOrderedGuids()
    {
        // Arrange — horloge deterministe pour eviter le flaky timing sur CI
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        int callCount = 0;
        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(_ => baseTime.AddMilliseconds(callCount++));
        SequentialGuidGenerator generator = CreateGenerator(SequentialGuidType.SequentialAsString, clock);
        List<string> guids = [];

        // Act
        for (int i = 0; i < 5; i++)
        {
            guids.Add(generator.Create().ToString());
        }

        // Assert - les representations string doivent etre en ordre croissant
        guids.ShouldBeInOrder(SortDirection.Ascending);
    }

    [Fact]
    public void Create_SequentialAtEnd_GeneratesNonEmptyGuids()
    {
        // Arrange
        SequentialGuidGenerator generator = CreateGenerator(SequentialGuidType.SequentialAtEnd);

        // Act
        Guid guid = generator.Create();

        // Assert
        guid.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_SequentialAsBinary_GeneratesNonEmptyGuids()
    {
        // Arrange
        SequentialGuidGenerator generator = CreateGenerator(SequentialGuidType.SequentialAsBinary);

        // Act
        Guid guid = generator.Create();

        // Assert
        guid.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_DefaultsToSequentialAsString()
    {
        // Arrange — horloge deterministe, pas de type specifie (defaut = SequentialAsString)
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        int callCount = 0;
        IClock clock = Substitute.For<IClock>();
        clock.Now.Returns(_ => baseTime.AddMilliseconds(callCount++));
        SequentialGuidGenerator generator = CreateGenerator(clock: clock);
        List<string> guids = [];

        // Act
        for (int i = 0; i < 5; i++)
        {
            guids.Add(generator.Create().ToString());
        }

        // Assert - si le defaut est SequentialAsString, les strings sont ordonnees
        guids.ShouldBeInOrder(SortDirection.Ascending);
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
            comparison.ShouldBeGreaterThanOrEqualTo(0,
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
