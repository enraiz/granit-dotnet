using Granit.Notifications.Sms.AzureCommunicationServices.Extensions;
using Granit.Notifications.Sms.AzureCommunicationServices.Internal;
using Granit.Notifications.Sms.AzureCommunicationServices.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Sms.AzureCommunicationServices.Tests;

public sealed class AcsSmsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitNotificationsSmsAcs_RegistersKeyedSmsSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsAcs();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(ISmsSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsSmsAcs_RegistersAcsSmsOptionsValidator()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsAcs();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<AcsSmsOptions>));
    }

    [Fact]
    public void AddGranitNotificationsSmsAcs_AppliesConfigureDelegate()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddGranitNotificationsSmsAcs(opts =>
        {
            opts.ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdA==";
            opts.FromPhoneNumber = "+15551234567";
        });

        ServiceProvider sp = services.BuildServiceProvider();
        AcsSmsOptions options = sp.GetRequiredService<IOptions<AcsSmsOptions>>().Value;

        options.ConnectionString.ShouldNotBeNull();
        options.FromPhoneNumber.ShouldBe("+15551234567");
    }

    [Fact]
    public void AddGranitNotificationsSmsAcs_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitNotificationsSmsAcs();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsSmsAcs_WithNullConfigure_DoesNotThrow()
    {
        ServiceCollection services = new();

        Should.NotThrow(() => services.AddGranitNotificationsSmsAcs(configure: null));
    }

    [Fact]
    public void AddGranitNotificationsSmsAcs_RegistersTransport()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsAcs();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IAcsSmsTransport) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }
}
