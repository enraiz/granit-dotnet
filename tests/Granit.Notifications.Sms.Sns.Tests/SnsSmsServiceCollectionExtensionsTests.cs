using Granit.Notifications.Sms.Sns.Extensions;
using Granit.Notifications.Sms.Sns.HealthChecks;
using Granit.Notifications.Sms.Sns.Internal;
using Granit.Notifications.Sms.Sns.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Sms.Sns.Tests;

public sealed class SnsSmsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitNotificationsSmsSns_RegistersKeyedSmsSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsSns();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(ISmsSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsSmsSns_RegistersOptionsValidator()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsSns();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<SnsSmsOptions>));
    }

    [Fact]
    public void AddGranitNotificationsSmsSns_RegistersTransport()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsSns();

        services.ShouldContain(d =>
            d.ServiceType == typeof(ISnsSmsTransport) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsSmsSns_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitNotificationsSmsSns();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsSmsSns_ConfigureDelegateIsApplied()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsSns(opts => opts.Region = "us-east-1");

        services.ShouldContain(d =>
            d.ServiceType == typeof(IConfigureOptions<SnsSmsOptions>));
    }

    [Fact]
    public void AddGranitNotificationsSmsSns_NullConfigureDoesNotThrow()
    {
        ServiceCollection services = new();
        Should.NotThrow(() => services.AddGranitNotificationsSmsSns(null));
    }

    [Fact]
    public void AddGranitSnsSmsHealthCheck_RegistersHealthCheck()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsSns();
        services.AddHealthChecks().AddGranitSnsSmsHealthCheck();

        services.ShouldContain(d =>
            d.ServiceType == typeof(SnsSmsHealthCheck));
    }
}
