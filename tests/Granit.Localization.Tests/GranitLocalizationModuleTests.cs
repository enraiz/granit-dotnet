using System.Globalization;
using FluentAssertions;
using Granit.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Granit.Localization.Tests;

public sealed class GranitLocalizationModuleTests : IDisposable
{
    private readonly CultureInfo _originalUICulture = CultureInfo.CurrentUICulture;

    public void Dispose() =>
        CultureInfo.CurrentUICulture = _originalUICulture;

    [Fact]
    public void ConfigureServices_RegistersStringLocalizerFactory()
    {
        // Arrange
        GranitLocalizationModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);
        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        IStringLocalizerFactory? factory = sp.GetService<IStringLocalizerFactory>();
        factory.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_RegistersGenericStringLocalizer()
    {
        // Arrange
        GranitLocalizationModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);
        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        IStringLocalizer<GranitLocalizationResource>? localizer =
            sp.GetService<IStringLocalizer<GranitLocalizationResource>>();
        localizer.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ResolvesGranitTranslation_Fr()
    {
        // Arrange
        GranitLocalizationModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        module.ConfigureServices(context);
        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        IStringLocalizer<GranitLocalizationResource> localizer =
            sp.GetRequiredService<IStringLocalizer<GranitLocalizationResource>>();
        LocalizedString result = localizer["Foundation:EntityNotFound", "Patient", "123"];

        // Assert
        result.ResourceNotFound.Should().BeFalse();
        result.Value.Should().Contain("Patient");
        result.Value.Should().Contain("123");
        result.Value.Should().Contain("entité");
    }

    [Fact]
    public void ConfigureServices_ResolvesGranitTranslation_En()
    {
        // Arrange
        GranitLocalizationModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        module.ConfigureServices(context);
        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        CultureInfo.CurrentUICulture = new CultureInfo("en");

        // Act
        IStringLocalizer<GranitLocalizationResource> localizer =
            sp.GetRequiredService<IStringLocalizer<GranitLocalizationResource>>();
        LocalizedString result = localizer["Foundation:EntityNotFound", "Patient", "123"];

        // Assert
        result.ResourceNotFound.Should().BeFalse();
        result.Value.Should().Contain("Patient");
        result.Value.Should().Contain("123");
        result.Value.Should().Contain("Entity");
    }

    [Fact]
    public void ConfigureServices_ResolvesAllGranitKeys()
    {
        // Arrange
        GranitLocalizationModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        module.ConfigureServices(context);
        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        IStringLocalizer<GranitLocalizationResource> localizer =
            sp.GetRequiredService<IStringLocalizer<GranitLocalizationResource>>();

        // Assert
        localizer["Foundation:ValidationError"].ResourceNotFound.Should().BeFalse();
        localizer["Foundation:Unauthorized"].ResourceNotFound.Should().BeFalse();
        localizer["Foundation:Forbidden"].ResourceNotFound.Should().BeFalse();
        localizer["Foundation:InternalError"].ResourceNotFound.Should().BeFalse();
    }
}
