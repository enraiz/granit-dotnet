using Granit.Notifications.MobilePush.Sns.Extensions;
using Granit.Notifications.MobilePush.Sns.HealthChecks;
using Granit.Notifications.MobilePush.Sns.Internal;
using Granit.Notifications.MobilePush.Sns.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.MobilePush.Sns.Tests;

public sealed class SnsMobilePushServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitNotificationsMobilePushSns_RegistersKeyedPushSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsMobilePushSns();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(IMobilePushSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsMobilePushSns_RegistersOptionsValidator()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsMobilePushSns();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<SnsMobilePushOptions>));
    }

    [Fact]
    public void AddGranitNotificationsMobilePushSns_RegistersTransport()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsMobilePushSns();

        services.ShouldContain(d =>
            d.ServiceType == typeof(ISnsMobilePushTransport) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsMobilePushSns_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitNotificationsMobilePushSns();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsMobilePushSns_ConfigureDelegateIsApplied()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsMobilePushSns(opts => opts.Region = "us-east-1");

        services.ShouldContain(d =>
            d.ServiceType == typeof(IConfigureOptions<SnsMobilePushOptions>));
    }

    [Fact]
    public void AddGranitNotificationsMobilePushSns_NullConfigureDoesNotThrow()
    {
        ServiceCollection services = new();
        Should.NotThrow(() => services.AddGranitNotificationsMobilePushSns(null));
    }

    [Fact]
    public void AddGranitSnsMobilePushHealthCheck_RegistersHealthCheck()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsMobilePushSns();
        services.AddHealthChecks().AddGranitSnsMobilePushHealthCheck();

        services.ShouldContain(d =>
            d.ServiceType == typeof(SnsMobilePushHealthCheck));
    }
}
