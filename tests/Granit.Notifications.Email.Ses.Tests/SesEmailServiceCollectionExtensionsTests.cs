using Granit.Notifications.Email.Ses.Extensions;
using Granit.Notifications.Email.Ses.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Email.Ses.Tests;

public sealed class SesEmailServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitNotificationsEmailSes_RegistersKeyedEmailSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsEmailSes();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(IEmailSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsEmailSes_RegistersSesOptionsValidator()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsEmailSes();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<SesOptions>));
    }

    [Fact]
    public void AddGranitNotificationsEmailSes_AppliesConfigureDelegate()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddGranitNotificationsEmailSes(opts =>
        {
            opts.Region = "eu-central-1";
            opts.FromAddress = "test@example.com";
        });

        ServiceProvider sp = services.BuildServiceProvider();
        SesOptions options = sp.GetRequiredService<IOptions<SesOptions>>().Value;

        options.Region.ShouldBe("eu-central-1");
        options.FromAddress.ShouldBe("test@example.com");
    }

    [Fact]
    public void AddGranitNotificationsEmailSes_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitNotificationsEmailSes();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsEmailSes_WithNullConfigure_DoesNotThrow()
    {
        ServiceCollection services = new();

        Should.NotThrow(() => services.AddGranitNotificationsEmailSes(configure: null));
    }

    [Fact]
    public void AddGranitNotificationsEmailSes_RegistersTransportFactory()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsEmailSes();

        services.ShouldContain(d =>
            d.ServiceType == typeof(Func<ISesTransport>) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }
}
