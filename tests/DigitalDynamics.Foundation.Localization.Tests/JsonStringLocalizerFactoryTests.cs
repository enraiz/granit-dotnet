using System.Globalization;
using DigitalDynamics.Foundation.Localization.Json;
using DigitalDynamics.Foundation.Localization.Tests.TestResources;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

using Xunit;

namespace DigitalDynamics.Foundation.Localization.Tests;

public sealed class JsonStringLocalizerFactoryTests : IDisposable
{
    private readonly CultureInfo _originalUICulture = CultureInfo.CurrentUICulture;

    public void Dispose() =>
        CultureInfo.CurrentUICulture = _originalUICulture;

    private static JsonStringLocalizerFactory CreateFactory(
        Action<FoundationLocalizationOptions>? configure = null)
    {
        ServiceCollection services = new();
        services.Configure<FoundationLocalizationOptions>(options =>
        {
            options.Resources
                .Add<TestResource>("fr")
                .AddJson(
                    typeof(JsonStringLocalizerFactoryTests).Assembly,
                    "DigitalDynamics.Foundation.Localization.Tests.TestResources.Localization.Test");

            options.Resources
                .Add<ParentTestResource>("fr")
                .AddJson(
                    typeof(JsonStringLocalizerFactoryTests).Assembly,
                    "DigitalDynamics.Foundation.Localization.Tests.TestResources.Localization.Parent");

            configure?.Invoke(options);
        });

        ServiceProvider sp = services.BuildServiceProvider();
        IOptions<FoundationLocalizationOptions> opts = sp.GetRequiredService<IOptions<FoundationLocalizationOptions>>();
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
        localizer["Test:Hello"].Value.Should().Be("Bonjour");
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
        localizer1.Should().BeSameAs(localizer2);
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
        localizer["Any:Key"].ResourceNotFound.Should().BeTrue();
        localizer["Any:Key"].Value.Should().Be("Any:Key");
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
        localizer["Parent:SharedKey"].Value.Should().Be("Valeur partagée du parent");
        localizer["Parent:SharedKey"].ResourceNotFound.Should().BeFalse();
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
        localizer["Parent:OnlyInParent"].Value.Should().Be("Seulement dans le parent");
        localizer["Parent:OnlyInParent"].ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Create_ByNameAndLocation_FallsBackToEmptyLocalizer()
    {
        // Arrange
        JsonStringLocalizerFactory factory = CreateFactory();

        // Act
        IStringLocalizer localizer = factory.Create("NonExistent.Type", "NonExistent.Assembly");

        // Assert
        localizer["Any:Key"].ResourceNotFound.Should().BeTrue();
    }
}
