using Granit.Notifications.Sms;
using Granit.Notifications.Twilio.Extensions;
using Granit.Notifications.Twilio.Internal;
using Granit.Notifications.Twilio.Options;
using Granit.Notifications.WhatsApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Twilio.Tests;

public sealed class TwilioNotificationsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitNotificationsTwilio_RegistersTwilioOptions()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsTwilio();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IConfigureOptions<TwilioOptions>));
    }

    [Fact]
    public void AddGranitNotificationsTwilio_RegistersTwilioNotificationProvider_AsSingleton()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsTwilio();

        services.ShouldContain(d =>
            d.ServiceType == typeof(TwilioNotificationProvider) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsTwilio_RegistersKeyedSmsSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsTwilio();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(ISmsSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsTwilio_RegistersKeyedWhatsAppSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsTwilio();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(IWhatsAppSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsTwilio_AppliesConfigureDelegate()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddGranitNotificationsTwilio(opts =>
        {
            opts.AccountSid = "AC_custom_sid";
            opts.AuthToken = "custom-token";
            opts.DefaultSmsFromNumber = "+19991234567";
        });

        ServiceProvider sp = services.BuildServiceProvider();
        TwilioOptions options = sp.GetRequiredService<IOptions<TwilioOptions>>().Value;

        options.AccountSid.ShouldBe("AC_custom_sid");
        options.AuthToken.ShouldBe("custom-token");
        options.DefaultSmsFromNumber.ShouldBe("+19991234567");
    }

    [Fact]
    public void AddGranitNotificationsTwilio_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitNotificationsTwilio();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsTwilio_WithNullConfigure_DoesNotThrow()
    {
        ServiceCollection services = new();

        Should.NotThrow(() => services.AddGranitNotificationsTwilio(configure: null));
    }

    [Fact]
    public void AddGranitNotificationsTwilio_RegistersHttpClient()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsTwilio();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IHttpClientFactory));
    }

    [Fact]
    public void AddGranitNotificationsTwilio_HttpClient_ConfiguresBaseAddressAndHeaders()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddGranitNotificationsTwilio(opts =>
        {
            opts.AccountSid = "AC_test_sid";
            opts.AuthToken = "test-token";
            opts.DefaultSmsFromNumber = "+15551234567";
            opts.BaseUrl = "https://api.twilio.com";
            opts.TimeoutSeconds = 15;
        });

        ServiceProvider sp = services.BuildServiceProvider();
        IHttpClientFactory factory = sp.GetRequiredService<IHttpClientFactory>();
        HttpClient client = factory.CreateClient("Twilio");

        client.BaseAddress.ShouldNotBeNull();
        client.BaseAddress!.ToString().ShouldEndWith("/");
        client.DefaultRequestHeaders.Authorization.ShouldNotBeNull();
        client.DefaultRequestHeaders.Authorization!.Scheme.ShouldBe("Basic");
        client.DefaultRequestHeaders.Contains("Accept").ShouldBeTrue();
    }

    [Fact]
    public void AddGranitNotificationsTwilio_HttpClient_TrimsTrailingSlashFromBaseUrl()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddGranitNotificationsTwilio(opts =>
        {
            opts.AccountSid = "AC_test_sid";
            opts.AuthToken = "test-token";
            opts.DefaultSmsFromNumber = "+15551234567";
            opts.BaseUrl = "https://api.twilio.com/";
            opts.TimeoutSeconds = 10;
        });

        ServiceProvider sp = services.BuildServiceProvider();
        IHttpClientFactory factory = sp.GetRequiredService<IHttpClientFactory>();
        HttpClient client = factory.CreateClient("Twilio");

        client.BaseAddress!.ToString().ShouldBe("https://api.twilio.com/");
    }
}
