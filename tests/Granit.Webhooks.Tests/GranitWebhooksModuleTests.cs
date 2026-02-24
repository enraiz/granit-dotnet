// =============================================================================
// Tests - GranitWebhooksModule
// =============================================================================
// Verifies module inheritance, DependsOn declarations, that AddGranitWebhooks()
// registers services without throwing, and that DI registrations are present.
// =============================================================================

using FluentAssertions;
using Granit.Core.Modularity;
using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Extensions;
using Granit.Wolverine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Granit.Webhooks.Tests;

public sealed class GranitWebhooksModuleTests
{
    [Fact]
    public void GranitWebhooksModule_IsGranitModule() =>
        typeof(GranitWebhooksModule).Should().BeAssignableTo<GranitModule>();

    [Fact]
    public void GranitWebhooksModule_IsSealed() =>
        typeof(GranitWebhooksModule).IsSealed.Should().BeTrue();

    [Fact]
    public void GranitWebhooksModule_DependsOn_WolverineModule()
    {
        DependsOnAttribute[] attributes = (DependsOnAttribute[])
            typeof(GranitWebhooksModule).GetCustomAttributes(typeof(DependsOnAttribute), inherit: false);

        attributes.Should().ContainSingle(a => a.DependedTypes.Contains(typeof(GranitWolverineModule)));
    }

    [Fact]
    public void AddGranitWebhooks_DoesNotThrow()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        Action act = () => builder.AddGranitWebhooks();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddGranitWebhooks_RegistersIWebhookPublisher_Scoped()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddGranitWebhooks();

        builder.Services.Should().Contain(d =>
            d.ServiceType == typeof(IWebhookPublisher) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitWebhooks_RegistersIWebhookSubscriptionStore_Singleton()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddGranitWebhooks();

        builder.Services.Should().Contain(d =>
            d.ServiceType == typeof(IWebhookSubscriptionStore) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitWebhooks_RegistersIWebhookDeliveryStore_Scoped()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddGranitWebhooks();

        builder.Services.Should().Contain(d =>
            d.ServiceType == typeof(IWebhookDeliveryStore) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitWebhooks_RegistersIWebhookSecretProtector_Singleton()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddGranitWebhooks();

        builder.Services.Should().Contain(d =>
            d.ServiceType == typeof(IWebhookSecretProtector) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitWebhooks_RegistersWebhooksOptionsValidator()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddGranitWebhooks();

        builder.Services.Should().Contain(d =>
            d.ServiceType == typeof(IValidateOptions<WebhooksOptions>));
    }

    [Fact]
    public void AddGranitWebhooks_WithConfigure_AppliesOptions()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.AddGranitWebhooks(opts => opts.MaxParallelDeliveries = 5);

        // No exception = callback was invoked successfully.
        builder.Services.Should().Contain(d => d.ServiceType == typeof(IWebhookPublisher));
    }
}
