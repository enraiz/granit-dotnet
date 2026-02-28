using System.Globalization;
using Granit.Localization.Json;
using Granit.Localization.Tests.TestResources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Localization.Tests;

public sealed class JsonStringLocalizerFactoryTests : IDisposable
{
    private readonly CultureInfo _originalUICulture = CultureInfo.CurrentUICulture;

    public void Dispose() =>
        CultureInfo.CurrentUICulture = _originalUICulture;

    private static JsonStringLocalizerFactory CreateFactory(
        Action<GranitLocalizationOptions>? configure = null)
    {
        ServiceCollection services = new();
        services.Configure<GranitLocalizationOptions>(options =>
        {
            options.Resources
                .Add<TestResource>("fr")
                .AddJson(
                    typeof(JsonStringLocalizerFactoryTests).Assembly,
                    "Granit.Localization.Tests.TestResources.Localization.Test");

            options.Resources
                .Add<ParentTestResource>("fr")
                .AddJson(
                    typeof(JsonStringLocalizerFactoryTests).Assembly,
                    "Granit.Localization.Tests.TestResources.Localization.Parent");

            configure?.Invoke(options);
        });

        ServiceProvider sp = services.BuildServiceProvider();
        IOptions<GranitLocalizationOptions> opts = sp.GetRequiredService<IOptions<GranitLocalizationOptions>>();
        return new JsonStringLocalizerFactory(opts);
    }

    [Fact]
    public void Create_ByType_ReturnsLocalizer()
    {
        // Arrange
        JsonStringLocalizerFactory factory = CreateFactory();
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        IStringLocalizer localizer = factory.Create(typeof(TestResource));

        // Assert
        localizer["Test:Hello"].Value.ShouldBe("Bonjour");
    }

    [Fact]
    public void Create_ByType_CachesSameInstance()
    {
        // Arrange
        JsonStringLocalizerFactory factory = CreateFactory();

        // Act
        IStringLocalizer localizer1 = factory.Create(typeof(TestResource));
        IStringLocalizer localizer2 = factory.Create(typeof(TestResource));

        // Assert
        localizer1.ShouldBeSameAs(localizer2);
    }

    [Fact]
    public void Create_UnregisteredType_ReturnsEmptyLocalizer()
    {
        // Arrange
        JsonStringLocalizerFactory factory = CreateFactory();
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        IStringLocalizer localizer = factory.Create(typeof(UnregisteredResource));

        // Assert
        localizer["Any:Key"].ResourceNotFound.ShouldBeTrue();
        localizer["Any:Key"].Value.ShouldBe("Any:Key");
    }

    [Fact]
    public void Create_WithInheritance_ResolvesParentKeys()
    {
        // Arrange
        JsonStringLocalizerFactory factory = CreateFactory(options =>
        {
            options.Resources
                .Add<ChildTestResource>("fr")
                .AddBaseTypes(typeof(ParentTestResource));
        });
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        IStringLocalizer localizer = factory.Create(typeof(ChildTestResource));

        // Assert — la clé du parent est accessible via héritage
        localizer["Parent:SharedKey"].Value.ShouldBe("Valeur partagée du parent");
        localizer["Parent:SharedKey"].ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithInheritResourceAttribute_ResolvesParentKeys()
    {
        // Arrange — ChildTestResource a [InheritResource(typeof(ParentTestResource))]
        JsonStringLocalizerFactory factory = CreateFactory(options =>
        {
            options.Resources
                .Add<ChildTestResource>("fr");
        });
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        IStringLocalizer localizer = factory.Create(typeof(ChildTestResource));

        // Assert
        localizer["Parent:OnlyInParent"].Value.ShouldBe("Seulement dans le parent");
        localizer["Parent:OnlyInParent"].ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Create_ByResourceName_ResolvesRegisteredResource()
    {
        // Arrange — TestResource is registered and carries [LocalizationResourceName("Test")].
        // GranitExceptionHandler calls Create(resourceName, assemblyName) where resourceName
        // is the prefix extracted from the error code (e.g. "BlobStorage" from "BlobStorage:NotFound").
        JsonStringLocalizerFactory factory = CreateFactory();
        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act — resolve by [LocalizationResourceName] name, not by CLR type name
        IStringLocalizer localizer = factory.Create("Test", "any.location");

        // Assert
        localizer["Test:Hello"].Value.ShouldBe("Bonjour");
        localizer["Test:Hello"].ResourceNotFound.ShouldBeFalse();
    }

    [Fact]
    public void Create_ByNameAndLocation_FallsBackToEmptyLocalizer()
    {
        // Arrange
        JsonStringLocalizerFactory factory = CreateFactory();

        // Act
        IStringLocalizer localizer = factory.Create("NonExistent.Type", "NonExistent.Assembly");

        // Assert
        localizer["Any:Key"].ResourceNotFound.ShouldBeTrue();
    }
}
