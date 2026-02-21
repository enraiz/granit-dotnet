using System.Globalization;
using DigitalDynamics.Foundation.Localization.Json;
using DigitalDynamics.Foundation.Localization.Tests.TestResources;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

using Xunit;

namespace DigitalDynamics.Foundation.Localization.Tests;

public sealed class JsonStringLocalizerTests : IDisposable
{
    private readonly CultureInfo _originalUICulture = CultureInfo.CurrentUICulture;

    public void Dispose() =>
        CultureInfo.CurrentUICulture = _originalUICulture;

    private static IStringLocalizer CreateTestLocalizer()
    {
        ServiceCollection services = new();
        services.Configure<FoundationLocalizationOptions>(options =>
        {
            options.Resources
                .Add<TestResource>("fr")
                .AddJson(
                    typeof(JsonStringLocalizerTests).Assembly,
                    "DigitalDynamics.Foundation.Localization.Tests.TestResources.Localization.Test");
        });

        ServiceProvider sp = services.BuildServiceProvider();
        IOptions<FoundationLocalizationOptions> opts = sp.GetRequiredService<IOptions<FoundationLocalizationOptions>>();
        JsonStringLocalizerFactory factory = new(opts);
        return factory.Create(typeof(TestResource));
    }

    [Fact]
    public void Indexer_ReturnsTranslation_WhenKeyExists()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        LocalizedString result = localizer["Test:Hello"];

        // Assert
        result.Value.Should().Be("Bonjour");
        result.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_ReturnsEnglishTranslation_WhenCultureIsEn()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("en");

        // Act
        LocalizedString result = localizer["Test:Hello"];

        // Assert
        result.Value.Should().Be("Hello");
        result.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_FallsBackToParentCulture()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("fr-CA");

        // Act — fr-CA n'existe pas, doit fallback sur fr
        LocalizedString result = localizer["Test:Hello"];

        // Assert
        result.Value.Should().Be("Bonjour");
        result.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_FallsBackToDefaultCulture_WhenNoCultureMatch()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("de");

        // Act — de n'existe pas, doit fallback sur fr (defaultCulture)
        LocalizedString result = localizer["Test:Hello"];

        // Assert
        result.Value.Should().Be("Bonjour");
        result.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_ReturnsKey_WhenKeyNotFound()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        LocalizedString result = localizer["NonExistent:Key"];

        // Assert
        result.Value.Should().Be("NonExistent:Key");
        result.ResourceNotFound.Should().BeTrue();
    }

    [Fact]
    public void Indexer_WithArguments_FormatsString()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        LocalizedString result = localizer["Test:Welcome", "Jean", 5];

        // Assert
        result.Value.Should().Be("Bienvenue Jean, vous avez 5 messages");
        result.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_WithArguments_ReturnsKey_WhenNotFound()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        LocalizedString result = localizer["Missing", "arg"];

        // Assert
        result.Value.Should().Be("Missing");
        result.ResourceNotFound.Should().BeTrue();
    }

    [Fact]
    public void GetAllStrings_ReturnsAllKeys()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        List<LocalizedString> all = localizer.GetAllStrings(includeParentCultures: false).ToList();

        // Assert
        all.Should().HaveCount(3);
        all.Select(s => s.Name).Should().Contain("Test:Hello");
        all.Select(s => s.Name).Should().Contain("Test:Welcome");
        all.Select(s => s.Name).Should().Contain("Test:Goodbye");
    }
}
