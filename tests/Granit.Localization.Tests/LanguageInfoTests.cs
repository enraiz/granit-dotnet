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
    }

    [Fact]
    public void Constructor_WithoutFlagIcon_DefaultsToNull()
    {
        LanguageInfo lang = new("en", "English");

        lang.CultureName.Should().Be("en");
        lang.DisplayName.Should().Be("English");
        lang.FlagIcon.Should().BeNull();
    }

    [Fact]
    public void Constructor_RegionalCulture_SetsCorrectly()
    {
        LanguageInfo lang = new("fr-CA", "Français (Canada)", "ca");

        lang.CultureName.Should().Be("fr-CA");
        lang.DisplayName.Should().Be("Français (Canada)");
        lang.FlagIcon.Should().Be("ca");
    }
}
