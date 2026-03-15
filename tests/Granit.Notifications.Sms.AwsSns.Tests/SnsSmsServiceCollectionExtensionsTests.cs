using Granit.Notifications.Sms.AwsSns.Extensions;
using Granit.Notifications.Sms.AwsSns.HealthChecks;
using Granit.Notifications.Sms.AwsSns.Internal;
using Granit.Notifications.Sms.AwsSns.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Sms.AwsSns.Tests;

public sealed class SnsSmsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitNotificationsSmsAwsSns_RegistersKeyedSmsSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsAwsSns();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(ISmsSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsSmsAwsSns_RegistersOptionsValidator()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsAwsSns();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<AwsSnsSmsOptions>));
    }

    [Fact]
    public void AddGranitNotificationsSmsAwsSns_RegistersTransport()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsAwsSns();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IAwsSnsSmsTransport) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsSmsAwsSns_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitNotificationsSmsAwsSns();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsSmsAwsSns_ConfigureDelegateIsApplied()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsAwsSns(opts => opts.Region = "us-east-1");

        services.ShouldContain(d =>
            d.ServiceType == typeof(IConfigureOptions<AwsSnsSmsOptions>));
    }

    [Fact]
    public void AddGranitNotificationsSmsAwsSns_NullConfigureDoesNotThrow()
    {
        ServiceCollection services = new();
        Should.NotThrow(() => services.AddGranitNotificationsSmsAwsSns(null));
    }

    [Fact]
    public void AddGranitAwsSnsSmsHealthCheck_RegistersHealthCheck()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsSmsAwsSns();
        services.AddHealthChecks().AddGranitAwsSnsSmsHealthCheck();

        services.ShouldContain(d =>
            d.ServiceType == typeof(AwsSnsSmsHealthCheck));
    }
}
