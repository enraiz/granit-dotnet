using FluentAssertions;
using Xunit;

namespace Granit.Localization.Tests;

public sealed class LanguageInfoTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        LanguageInfo lang = new("fr", "Français", "fr");

        lang.CultureName.Should().Be("fr");
        lang.DisplayName.Should().Be("Français");
        lang.FlagIcon.Should().Be("fr");
        lang.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithoutFlagIcon_DefaultsToNull()
    {
        LanguageInfo lang = new("en", "English");

        lang.CultureName.Should().Be("en");
        lang.DisplayName.Should().Be("English");
        lang.FlagIcon.Should().BeNull();
        lang.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Constructor_RegionalCulture_SetsCorrectly()
    {
        LanguageInfo lang = new("fr-CA", "Français (Canada)", "ca");

        lang.CultureName.Should().Be("fr-CA");
        lang.DisplayName.Should().Be("Français (Canada)");
        lang.FlagIcon.Should().Be("ca");
        lang.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithIsDefault_SetsProperty()
    {
        LanguageInfo lang = new("en", "English", "gb", isDefault: true);

        lang.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsEverything()
    {
        LanguageInfo lang = new("en", "English", "gb", isDefault: true);

        lang.CultureName.Should().Be("en");
        lang.DisplayName.Should().Be("English");
        lang.FlagIcon.Should().Be("gb");
        lang.IsDefault.Should().BeTrue();
    }
}
