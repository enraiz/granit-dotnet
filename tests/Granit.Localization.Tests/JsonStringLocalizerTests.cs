using System.Globalization;
using Granit.Localization.Json;
using Granit.Localization.Options;
using Granit.Localization.Tests.TestResources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Localization.Tests;

public sealed class JsonStringLocalizerTests : IDisposable
{
    private readonly CultureInfo _originalUICulture = CultureInfo.CurrentUICulture;

    public void Dispose() =>
        CultureInfo.CurrentUICulture = _originalUICulture;

    private static IStringLocalizer CreateTestLocalizer()
    {
        ServiceCollection services = new();
        services.Configure<GranitLocalizationOptions>(options =>
        {
            options.Resources
                .Add<TestResource>("fr")
                .AddJson(
                    typeof(JsonStringLocalizerTests).Assembly,
                    "Granit.Localization.Tests.TestResources.Localization.Test");
        });

        ServiceProvider sp = services.BuildServiceProvider();
        IOptions<GranitLocalizationOptions> opts = sp.GetRequiredService<IOptions<GranitLocalizationOptions>>();
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
        result.Value.ShouldBe("Bonjour");
        result.ResourceNotFound.ShouldBeFalse();
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
        result.Value.ShouldBe("Hello");
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Indexer_ReturnsRegionalOverride_WhenKeyExistsInRegionalFile()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("fr-CA");

        // Act — fr-CA.json has "Allô" override for Test:Hello
        LocalizedString result = localizer["Test:Hello"];

        // Assert
        result.Value.ShouldBe("Allô");
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Indexer_FallsBackToParentCulture_WhenKeyNotInRegionalFile()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("fr-CA");

        // Act — fr-CA.json does not have Test:Goodbye, must fallback to fr.json
        LocalizedString result = localizer["Test:Goodbye"];

        // Assert
        result.Value.ShouldBe("Au revoir");
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Indexer_ReturnsEnGbOverride_WhenKeyExistsInRegionalFile()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("en-GB");

        // Act — en-GB.json has "Hiya" override for Test:Hello
        LocalizedString result = localizer["Test:Hello"];

        // Assert
        result.Value.ShouldBe("Hiya");
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Indexer_EnGb_FallsBackToEn_WhenKeyNotInRegionalFile()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("en-GB");

        // Act — en-GB.json does not have Test:Goodbye, must fallback to en.json
        LocalizedString result = localizer["Test:Goodbye"];

        // Assert
        result.Value.ShouldBe("Goodbye");
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Indexer_EnUs_FallsBackToEn_WhenNoRegionalFileExists()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");

        // Act — no en-US.json file, must fallback to en.json via CultureInfo.Parent
        LocalizedString result = localizer["Test:Hello"];

        // Assert
        result.Value.ShouldBe("Hello");
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Indexer_FallsBackToParentCulture_WhenNoRegionalFileExists()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("fr-BE");

        // Act — fr-BE.json does not exist, must fallback to fr.json
        LocalizedString result = localizer["Test:Hello"];

        // Assert
        result.Value.ShouldBe("Bonjour");
        result.ResourceNotFound.ShouldBeFalse();
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
        result.Value.ShouldBe("Bonjour");
        result.ResourceNotFound.ShouldBeFalse();
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
        result.Value.ShouldBe("NonExistent:Key");
        result.ResourceNotFound.ShouldBeTrue();
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
        result.Value.ShouldBe("Bienvenue Jean, vous avez 5 messages");
        result.ResourceNotFound.ShouldBeFalse();
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
        result.Value.ShouldBe("Missing");
        result.ResourceNotFound.ShouldBeTrue();
    }

    [Fact]
    public void GetAllStrings_ReturnsAllKeys()
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        var all = localizer.GetAllStrings(includeParentCultures: false).ToList();

        // Assert
        all.Count.ShouldBe(4);
        all.Select(s => s.Name).ShouldContain("Test:Hello");
        all.Select(s => s.Name).ShouldContain("Test:Welcome");
        all.Select(s => s.Name).ShouldContain("Test:Goodbye");
        all.Select(s => s.Name).ShouldContain("Test:Files:Count");
    }

    [Theory]
    [InlineData(0, "Aucun fichier")]
    [InlineData(1, "Un fichier")]
    [InlineData(5, "5 fichiers")]
    [InlineData(100, "100 fichiers")]
    public void Indexer_WithPluralization_FormatsCorrectly_Fr(int count, string expected)
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("fr");
        CultureInfo.CurrentCulture = new CultureInfo("fr");

        // Act
        LocalizedString result = localizer["Test:Files:Count", count];

        // Assert
        result.Value.ShouldBe(expected);
        result.ResourceNotFound.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, "No file")]
    [InlineData(1, "One file")]
    [InlineData(5, "5 files")]
    [InlineData(100, "100 files")]
    public void Indexer_WithPluralization_FormatsCorrectly_En(int count, string expected)
    {
        // Arrange
        IStringLocalizer localizer = CreateTestLocalizer();
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");

        // Act
        LocalizedString result = localizer["Test:Files:Count", count];

        // Assert
        result.Value.ShouldBe(expected);
        result.ResourceNotFound.ShouldBeFalse();
    }
}
