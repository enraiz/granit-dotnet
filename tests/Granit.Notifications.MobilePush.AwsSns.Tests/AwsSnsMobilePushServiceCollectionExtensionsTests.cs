using Granit.Notifications.MobilePush.AwsSns.Extensions;
using Granit.Notifications.MobilePush.AwsSns.HealthChecks;
using Granit.Notifications.MobilePush.AwsSns.Internal;
using Granit.Notifications.MobilePush.AwsSns.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.MobilePush.AwsSns.Tests;

public sealed class AwsSnsMobilePushServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitNotificationsMobilePushAwsSns_RegistersKeyedPushSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsMobilePushAwsSns();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(IMobilePushSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsMobilePushAwsSns_RegistersOptionsValidator()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsMobilePushAwsSns();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<AwsSnsMobilePushOptions>));
    }

    [Fact]
    public void AddGranitNotificationsMobilePushAwsSns_RegistersTransport()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsMobilePushAwsSns();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IAwsSnsMobilePushTransport) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsMobilePushAwsSns_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitNotificationsMobilePushAwsSns();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsMobilePushAwsSns_ConfigureDelegateIsApplied()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsMobilePushAwsSns(opts => opts.Region = "us-east-1");

        services.ShouldContain(d =>
            d.ServiceType == typeof(IConfigureOptions<AwsSnsMobilePushOptions>));
    }

    [Fact]
    public void AddGranitNotificationsMobilePushAwsSns_NullConfigureDoesNotThrow()
    {
        ServiceCollection services = new();
        Should.NotThrow(() => services.AddGranitNotificationsMobilePushAwsSns(null));
    }

    [Fact]
    public void AddGranitAwsSnsMobilePushHealthCheck_RegistersHealthCheck()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsMobilePushAwsSns();
        services.AddHealthChecks().AddGranitAwsSnsMobilePushHealthCheck();

        services.ShouldContain(d =>
            d.ServiceType == typeof(AwsSnsMobilePushHealthCheck));
    }
}
