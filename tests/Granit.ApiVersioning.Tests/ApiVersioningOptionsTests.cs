// =============================================================================
// Tests - GranitApiVersioningOptions
// =============================================================================
// Vérifie les valeurs par défaut et la constante SectionName.
// =============================================================================

using FluentAssertions;
using Granit.ApiVersioning.Options;
using Xunit;

namespace Granit.ApiVersioning.Tests;

public sealed class GranitApiVersioningOptionsTests
{
    [Fact]
    public void SectionName_IsApiVersioning() =>
        GranitApiVersioningOptions.SectionName.Should().Be("ApiVersioning");

    [Fact]
    public void DefaultMajorVersion_DefaultsToOne()
    {
        GranitApiVersioningOptions options = new();

        options.DefaultMajorVersion.Should().Be(1);
    }

    [Fact]
    public void ReportApiVersions_DefaultsToTrue()
    {
        GranitApiVersioningOptions options = new();

        options.ReportApiVersions.Should().BeTrue();
    }
}
