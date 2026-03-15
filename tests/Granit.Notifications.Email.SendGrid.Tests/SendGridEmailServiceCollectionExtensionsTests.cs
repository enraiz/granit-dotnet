using Granit.Notifications.Email.SendGrid.Extensions;
using Granit.Notifications.Email.SendGrid.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Email.SendGrid.Tests;

public sealed class SendGridEmailServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitNotificationsEmailSendGrid_RegistersKeyedEmailSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsEmailSendGrid();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(IEmailSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsEmailSendGrid_AppliesConfigureDelegate()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddGranitNotificationsEmailSendGrid(opts =>
        {
            opts.ApiKey = "SG.test-key";
            opts.DefaultSenderEmail = "test@example.com";
            opts.DefaultSenderName = "Test";
        });

        ServiceProvider sp = services.BuildServiceProvider();
        SendGridEmailOptions options = sp.GetRequiredService<IOptions<SendGridEmailOptions>>().Value;

        options.ApiKey.ShouldBe("SG.test-key");
        options.DefaultSenderEmail.ShouldBe("test@example.com");
        options.DefaultSenderName.ShouldBe("Test");
    }

    [Fact]
    public void AddGranitNotificationsEmailSendGrid_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitNotificationsEmailSendGrid();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsEmailSendGrid_WithNullConfigure_DoesNotThrow()
    {
        ServiceCollection services = new();

        Should.NotThrow(() => services.AddGranitNotificationsEmailSendGrid(configure: null));
    }

    [Fact]
    public void AddGranitNotificationsEmailSendGrid_RegistersHttpClient()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsEmailSendGrid();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IHttpClientFactory));
    }
}
