using Granit.Notifications.Brevo.Extensions;
using Granit.Notifications.Brevo.Options;
using Granit.Notifications.Email;
using Granit.Notifications.Sms;
using Granit.Notifications.WhatsApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Brevo.Tests;

public sealed class BrevoNotificationsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitNotificationsBrevo_RegistersBrevoOptions()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsBrevo();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IConfigureOptions<BrevoOptions>));
    }

    [Fact]
    public void AddGranitNotificationsBrevo_RegistersBrevoNotificationProvider_AsSingleton()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsBrevo();

        services.ShouldContain(d =>
            d.ServiceType == typeof(BrevoNotificationProvider) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsBrevo_RegistersKeyedEmailSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsBrevo();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(IEmailSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsBrevo_RegistersKeyedSmsSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsBrevo();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(ISmsSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsBrevo_RegistersKeyedWhatsAppSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsBrevo();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(IWhatsAppSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsBrevo_AppliesConfigureDelegate()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddGranitNotificationsBrevo(opts =>
        {
            opts.ApiKey = "custom-key";
            opts.DefaultSenderEmail = "custom@test.com";
            opts.DefaultSenderName = "Custom";
        });

        ServiceProvider sp = services.BuildServiceProvider();
        BrevoOptions options = sp.GetRequiredService<IOptions<BrevoOptions>>().Value;

        options.ApiKey.ShouldBe("custom-key");
        options.DefaultSenderEmail.ShouldBe("custom@test.com");
        options.DefaultSenderName.ShouldBe("Custom");
    }

    [Fact]
    public void AddGranitNotificationsBrevo_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitNotificationsBrevo();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsBrevo_WithNullConfigure_DoesNotThrow()
    {
        ServiceCollection services = new();

        Should.NotThrow(() => services.AddGranitNotificationsBrevo(configure: null));
    }

    [Fact]
    public void AddGranitNotificationsBrevo_RegistersHttpClient()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsBrevo();

        // HttpClient registration adds IHttpClientFactory
        services.ShouldContain(d =>
            d.ServiceType == typeof(IHttpClientFactory));
    }

    [Fact]
    public void AddGranitNotificationsBrevo_HttpClient_ConfiguresBaseAddressAndHeaders()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddGranitNotificationsBrevo(opts =>
        {
            opts.ApiKey = "test-api-key";
            opts.DefaultSenderEmail = "sender@test.com";
            opts.BaseUrl = "https://api.brevo.com/v3";
            opts.TimeoutSeconds = 15;
        });

        ServiceProvider sp = services.BuildServiceProvider();
        IHttpClientFactory factory = sp.GetRequiredService<IHttpClientFactory>();
        HttpClient client = factory.CreateClient("Brevo");

        client.BaseAddress.ShouldNotBeNull();
        client.BaseAddress!.ToString().ShouldEndWith("/");
        client.DefaultRequestHeaders.Contains("api-key").ShouldBeTrue();
        client.DefaultRequestHeaders.Contains("Accept").ShouldBeTrue();
    }

    [Fact]
    public void AddGranitNotificationsBrevo_HttpClient_TrimsTrailingSlashFromBaseUrl()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddGranitNotificationsBrevo(opts =>
        {
            opts.ApiKey = "key";
            opts.DefaultSenderEmail = "sender@test.com";
            opts.BaseUrl = "https://api.brevo.com/v3/";
            opts.TimeoutSeconds = 10;
        });

        ServiceProvider sp = services.BuildServiceProvider();
        IHttpClientFactory factory = sp.GetRequiredService<IHttpClientFactory>();
        HttpClient client = factory.CreateClient("Brevo");

        // BaseUrl with trailing slash should still produce a single trailing slash
        client.BaseAddress!.ToString().ShouldBe("https://api.brevo.com/v3/");
    }
}
