// =============================================================================
// Tests - ApiDocumentationOptions
// =============================================================================
// Vérifie les valeurs par défaut et la constante SectionName.
// =============================================================================

using FluentAssertions;
using Granit.ApiDocumentation.Options;
using Xunit;

namespace Granit.ApiDocumentation.Tests;

public sealed class ApiDocumentationOptionsTests
{
    [Fact]
    public void SectionName_IsApiDocumentation() =>
        ApiDocumentationOptions.SectionName.Should().Be("ApiDocumentation");

    [Fact]
    public void MajorVersions_DefaultsToListWithOne()
    {
        ApiDocumentationOptions options = new();

        options.MajorVersions.Should().ContainSingle()
            .Which.Should().Be(1);
    }

    [Fact]
    public void Title_DefaultsToApi()
    {
        ApiDocumentationOptions options = new();

        options.Title.Should().Be("API");
    }

    [Fact]
    public void Description_DefaultsToNull()
    {
        ApiDocumentationOptions options = new();

        options.Description.Should().BeNull();
    }

    [Fact]
    public void ContactEmail_DefaultsToNull()
    {
        ApiDocumentationOptions options = new();

        options.ContactEmail.Should().BeNull();
    }

    [Fact]
    public void EnableInProduction_DefaultsToFalse()
    {
        ApiDocumentationOptions options = new();

        options.EnableInProduction.Should().BeFalse();
    }
}
