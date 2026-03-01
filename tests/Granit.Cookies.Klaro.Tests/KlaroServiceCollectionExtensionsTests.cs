using Granit.Cookies.Klaro.Extensions;
using Granit.Cookies.Klaro.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Cookies.Klaro.Tests;

public sealed class KlaroServiceCollectionExtensionsTests
{
    private static IConfiguration CreateConfiguration(
        string cookieName = "klaro",
        Dictionary<string, string>? serviceMappings = null)
    {
        Dictionary<string, string?> configData = new()
        {
            ["Klaro:CookieName"] = cookieName,
        };

        if (serviceMappings is not null)
        {
            foreach (KeyValuePair<string, string> mapping in serviceMappings)
            {
                configData[$"Klaro:ServiceMappings:{mapping.Key}"] = mapping.Value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public void AddGranitCookiesKlaro_RegistersConsentResolver()
    {
        ServiceCollection services = new();
        services.AddSingleton(CreateConfiguration(serviceMappings: new()
        {
            ["google-analytics"] = "Analytics",
        }));
        services.AddLogging();

        services.AddGranitCookiesKlaro();

        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IConsentResolver? resolver = scope.ServiceProvider.GetService<IConsentResolver>();
        resolver.ShouldNotBeNull();
        resolver.ShouldBeOfType<KlaroConsentResolver>();
    }

    [Fact]
    public void AddGranitCookiesKlaro_BindsOptions()
    {
        ServiceCollection services = new();
        services.AddSingleton(CreateConfiguration(
            cookieName: "my-consent",
            serviceMappings: new()
            {
                ["matomo"] = "Analytics",
                ["youtube"] = "Marketing",
            }));
        services.AddLogging();

        services.AddGranitCookiesKlaro();

        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<KlaroOptions> options = provider.GetRequiredService<IOptions<KlaroOptions>>();
        options.Value.CookieName.ShouldBe("my-consent");
        options.Value.ServiceMappings.ShouldContainKey("matomo");
        options.Value.ServiceMappings["matomo"].ShouldBe(CookieCategory.Analytics);
        options.Value.ServiceMappings["youtube"].ShouldBe(CookieCategory.Marketing);
    }

    [Fact]
    public void AddGranitCookiesKlaro_DefaultCookieName()
    {
        ServiceCollection services = new();
        services.AddSingleton(CreateConfiguration(serviceMappings: new()
        {
            ["ga"] = "Analytics",
        }));
        services.AddLogging();

        services.AddGranitCookiesKlaro();

        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<KlaroOptions> options = provider.GetRequiredService<IOptions<KlaroOptions>>();
        options.Value.CookieName.ShouldBe("klaro");
    }
}
