using System.Globalization;
using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

using Xunit;

namespace DigitalDynamics.Foundation.Localization.Tests;

public sealed class FoundationLocalizationModuleTests : IDisposable
{
    private readonly CultureInfo _originalUICulture = CultureInfo.CurrentUICulture;

    public void Dispose() =>
        CultureInfo.CurrentUICulture = _originalUICulture;

    [Fact]
    public void ConfigureServices_RegistersStringLocalizerFactory()
    {
        // Arrange
        FoundationLocalizationModule module = new();
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
        FoundationLocalizationModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);
        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        IStringLocalizer<FoundationLocalizationResource>? localizer =
            sp.GetService<IStringLocalizer<FoundationLocalizationResource>>();
        localizer.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ResolvesFoundationTranslation_Fr()
    {
        // Arrange
        FoundationLocalizationModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        module.ConfigureServices(context);
        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        IStringLocalizer<FoundationLocalizationResource> localizer =
            sp.GetRequiredService<IStringLocalizer<FoundationLocalizationResource>>();
        LocalizedString result = localizer["Foundation:EntityNotFound", "Patient", "123"];

        // Assert
        result.ResourceNotFound.Should().BeFalse();
        result.Value.Should().Contain("Patient");
        result.Value.Should().Contain("123");
        result.Value.Should().Contain("entité");
    }

    [Fact]
    public void ConfigureServices_ResolvesFoundationTranslation_En()
    {
        // Arrange
        FoundationLocalizationModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        module.ConfigureServices(context);
        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        CultureInfo.CurrentUICulture = new CultureInfo("en");

        // Act
        IStringLocalizer<FoundationLocalizationResource> localizer =
            sp.GetRequiredService<IStringLocalizer<FoundationLocalizationResource>>();
        LocalizedString result = localizer["Foundation:EntityNotFound", "Patient", "123"];

        // Assert
        result.ResourceNotFound.Should().BeFalse();
        result.Value.Should().Contain("Patient");
        result.Value.Should().Contain("123");
        result.Value.Should().Contain("Entity");
    }

    [Fact]
    public void ConfigureServices_ResolvesAllFoundationKeys()
    {
        // Arrange
        FoundationLocalizationModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        module.ConfigureServices(context);
        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        CultureInfo.CurrentUICulture = new CultureInfo("fr");

        // Act
        IStringLocalizer<FoundationLocalizationResource> localizer =
            sp.GetRequiredService<IStringLocalizer<FoundationLocalizationResource>>();

        // Assert
        localizer["Foundation:ValidationError"].ResourceNotFound.Should().BeFalse();
        localizer["Foundation:Unauthorized"].ResourceNotFound.Should().BeFalse();
        localizer["Foundation:Forbidden"].ResourceNotFound.Should().BeFalse();
        localizer["Foundation:InternalError"].ResourceNotFound.Should().BeFalse();
    }
}
