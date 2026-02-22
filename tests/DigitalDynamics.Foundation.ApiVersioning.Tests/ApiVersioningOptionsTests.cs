// =============================================================================
// Tests - FoundationApiVersioningOptions
// =============================================================================
// Vérifie les valeurs par défaut et la constante SectionName.
// =============================================================================

using DigitalDynamics.Foundation.ApiVersioning.Options;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.ApiVersioning.Tests;

public sealed class FoundationApiVersioningOptionsTests
{
    [Fact]
    public void SectionName_IsApiVersioning() =>
        FoundationApiVersioningOptions.SectionName.Should().Be("ApiVersioning");

    [Fact]
    public void DefaultMajorVersion_DefaultsToOne()
    {
        FoundationApiVersioningOptions options = new();

        options.DefaultMajorVersion.Should().Be(1);
    }

    [Fact]
    public void ReportApiVersions_DefaultsToTrue()
    {
        FoundationApiVersioningOptions options = new();

        options.ReportApiVersions.Should().BeTrue();
    }
}
