using Granit.Notifications.Email.Smtp.Extensions;
using Granit.Notifications.Email.Smtp.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Email.Smtp.Tests;

public sealed class SmtpEmailServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitNotificationsEmailSmtp_RegistersKeyedEmailSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsEmailSmtp();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(IEmailSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsEmailSmtp_RegistersSmtpOptions()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsEmailSmtp();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IConfigureOptions<SmtpOptions>));
    }

    [Fact]
    public void AddGranitNotificationsEmailSmtp_AppliesConfigureDelegate()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddGranitNotificationsEmailSmtp(opts =>
        {
            opts.Host = "mail.example.com";
            opts.Port = 465;
        });

        ServiceProvider sp = services.BuildServiceProvider();
        SmtpOptions options = sp.GetRequiredService<IOptions<SmtpOptions>>().Value;

        options.Host.ShouldBe("mail.example.com");
        options.Port.ShouldBe(465);
    }

    [Fact]
    public void AddGranitNotificationsEmailSmtp_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitNotificationsEmailSmtp();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsEmailSmtp_WithNullConfigure_DoesNotThrow()
    {
        ServiceCollection services = new();

        Should.NotThrow(() => services.AddGranitNotificationsEmailSmtp(configure: null));
    }
}
