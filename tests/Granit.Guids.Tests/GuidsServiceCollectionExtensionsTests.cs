// =============================================================================
// Tests - GuidsServiceCollectionExtensions
// =============================================================================
// Vérifie que AddGranitGuids enregistre les services attendus.
// =============================================================================

using FluentAssertions;
using Granit.Guids.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Granit.Guids.Tests;

public sealed class GuidsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitGuids_RegistersGuidGenerator()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddOptions();

        // Act
        services.AddGranitGuids();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IGuidGenerator? generator = sp.GetService<IGuidGenerator>();
        generator.Should().NotBeNull();
        generator.Should().BeOfType<SequentialGuidGenerator>();
    }

    [Fact]
    public void AddGranitGuids_TryAddSingleton_DoesNotOverrideExisting()
    {
        // Arrange
        ServiceCollection services = new();
        IGuidGenerator customGenerator = NSubstitute.Substitute.For<IGuidGenerator>();
        services.AddSingleton(customGenerator);

        // Act
        services.AddGranitGuids();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IGuidGenerator resolved = sp.GetRequiredService<IGuidGenerator>();
        resolved.Should().BeSameAs(customGenerator);
    }

    [Fact]
    public void AddGranitGuids_WithConfigure_AppliesOptions()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitGuids(opts =>
        {
            opts.DefaultSequentialGuidType = SequentialGuidType.SequentialAsBinary;
        });

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        GuidGeneratorOptions options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GuidGeneratorOptions>>().Value;
        options.DefaultSequentialGuidType.Should().Be(SequentialGuidType.SequentialAsBinary);
    }
}
