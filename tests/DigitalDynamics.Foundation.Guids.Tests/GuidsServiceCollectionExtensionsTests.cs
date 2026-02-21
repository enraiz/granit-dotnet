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
        ServiceCollection services = new();
        services.AddOptions();

        // Act
        services.AddFoundationGuids();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IGuidGenerator? generator = sp.GetService<IGuidGenerator>();
        generator.Should().NotBeNull();
        generator.Should().BeOfType<SequentialGuidGenerator>();
    }

    [Fact]
    public void AddFoundationGuids_TryAddSingleton_DoesNotOverrideExisting()
    {
        // Arrange
        ServiceCollection services = new();
        IGuidGenerator customGenerator = NSubstitute.Substitute.For<IGuidGenerator>();
        services.AddSingleton(customGenerator);

        // Act
        services.AddFoundationGuids();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IGuidGenerator resolved = sp.GetRequiredService<IGuidGenerator>();
        resolved.Should().BeSameAs(customGenerator);
    }

    [Fact]
    public void AddFoundationGuids_WithConfigure_AppliesOptions()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationGuids(opts =>
        {
            opts.DefaultSequentialGuidType = SequentialGuidType.SequentialAsBinary;
        });

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        GuidGeneratorOptions options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GuidGeneratorOptions>>().Value;
        options.DefaultSequentialGuidType.Should().Be(SequentialGuidType.SequentialAsBinary);
    }
}
