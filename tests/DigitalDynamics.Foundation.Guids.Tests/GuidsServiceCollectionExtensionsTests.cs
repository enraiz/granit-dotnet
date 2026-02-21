// =============================================================================
// Tests - GuidsServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationGuids enregistre les services attendus.
// =============================================================================

using DigitalDynamics.Foundation.Guids.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DigitalDynamics.Foundation.Guids.Tests;

public sealed class GuidsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFoundationGuids_RegistersGuidGenerator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();

        // Act
        services.AddFoundationGuids();

        using var sp = services.BuildServiceProvider();

        // Assert
        var generator = sp.GetService<IGuidGenerator>();
        generator.Should().NotBeNull();
        generator.Should().BeOfType<SequentialGuidGenerator>();
    }

    [Fact]
    public void AddFoundationGuids_TryAddSingleton_DoesNotOverrideExisting()
    {
        // Arrange
        var services = new ServiceCollection();
        var customGenerator = NSubstitute.Substitute.For<IGuidGenerator>();
        services.AddSingleton(customGenerator);

        // Act
        services.AddFoundationGuids();

        using var sp = services.BuildServiceProvider();

        // Assert
        var resolved = sp.GetRequiredService<IGuidGenerator>();
        resolved.Should().BeSameAs(customGenerator);
    }

    [Fact]
    public void AddFoundationGuids_WithConfigure_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFoundationGuids(opts =>
        {
            opts.DefaultSequentialGuidType = SequentialGuidType.SequentialAsBinary;
        });

        using var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GuidGeneratorOptions>>().Value;
        options.DefaultSequentialGuidType.Should().Be(SequentialGuidType.SequentialAsBinary);
    }
}
