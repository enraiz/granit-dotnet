using Granit.Notifications.Abstractions;
using Granit.Notifications.Zulip.Extensions;
using Granit.Notifications.Zulip.Internal;
using Granit.Notifications.Zulip.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Zulip.Tests;

public sealed class ZulipNotificationsServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddSingleton(configuration);
        services.AddLogging();
        return services;
    }

    [Fact]
    public void AddGranitNotificationsZulip_RegistersZulipSender()
    {
        ServiceCollection services = CreateServices();

        services.AddGranitNotificationsZulip(
            configureBot: bot =>
            {
                bot.BaseUrl = "https://zulip.test.com";
                bot.BotEmail = "bot@test.com";
                bot.ApiKey = "key";
            });

        ServiceProvider provider = services.BuildServiceProvider();
        IZulipSender? sender = provider.GetService<IZulipSender>();
        sender.ShouldNotBeNull();
        sender.ShouldBeOfType<ZulipBotSender>();
    }

    [Fact]
    public void AddGranitNotificationsZulip_RegistersNotificationChannel()
    {
        ServiceCollection services = CreateServices();

        services.AddGranitNotificationsZulip(
            configureBot: bot =>
            {
                bot.BaseUrl = "https://zulip.test.com";
                bot.BotEmail = "bot@test.com";
                bot.ApiKey = "key";
            });

        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        INotificationChannel? channel = scope.ServiceProvider.GetService<INotificationChannel>();
        channel.ShouldNotBeNull();
        channel.ShouldBeOfType<ZulipNotificationChannel>();
    }

    [Fact]
    public void AddGranitNotificationsZulip_ConfiguresChannelOptions()
    {
        ServiceCollection services = CreateServices();

        services.AddGranitNotificationsZulip(
            configureChannel: ch =>
            {
                ch.DefaultStream = "custom-stream";
                ch.DefaultTopic = "custom-topic";
            },
            configureBot: bot =>
            {
                bot.BaseUrl = "https://zulip.test.com";
                bot.BotEmail = "bot@test.com";
                bot.ApiKey = "key";
            });

        ServiceProvider provider = services.BuildServiceProvider();
        ZulipChannelOptions channelOptions = provider.GetRequiredService<IOptions<ZulipChannelOptions>>().Value;
        channelOptions.DefaultStream.ShouldBe("custom-stream");
        channelOptions.DefaultTopic.ShouldBe("custom-topic");
    }

    [Fact]
    public void AddGranitNotificationsZulip_ConfiguresBotOptions()
    {
        ServiceCollection services = CreateServices();

        services.AddGranitNotificationsZulip(
            configureBot: bot =>
            {
                bot.BaseUrl = "https://zulip.custom.com";
                bot.BotEmail = "custom-bot@test.com";
                bot.ApiKey = "custom-key";
                bot.TimeoutSeconds = 45;
            });

        ServiceProvider provider = services.BuildServiceProvider();
        ZulipBotOptions botOptions = provider.GetRequiredService<IOptions<ZulipBotOptions>>().Value;
        botOptions.BaseUrl.ShouldBe("https://zulip.custom.com");
        botOptions.BotEmail.ShouldBe("custom-bot@test.com");
        botOptions.ApiKey.ShouldBe("custom-key");
        botOptions.TimeoutSeconds.ShouldBe(45);
    }

    [Fact]
    public void AddGranitNotificationsZulip_WithNullConfigureDelegates_DoesNotThrow()
    {
        ServiceCollection services = CreateServices();

        Should.NotThrow(() => services.AddGranitNotificationsZulip());
    }

    [Fact]
    public void AddGranitNotificationsZulip_ReturnsServiceCollection()
    {
        ServiceCollection services = CreateServices();

        IServiceCollection result = services.AddGranitNotificationsZulip();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsZulip_RegistersHttpClient()
    {
        ServiceCollection services = CreateServices();

        services.AddGranitNotificationsZulip(
            configureBot: bot =>
            {
                bot.BaseUrl = "https://zulip.test.com";
                bot.BotEmail = "bot@test.com";
                bot.ApiKey = "key";
            });

        ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory? httpClientFactory = provider.GetService<IHttpClientFactory>();
        httpClientFactory.ShouldNotBeNull();
    }

    [Fact]
    public void AddGranitNotificationsZulip_HttpClientHasCorrectBaseAddress()
    {
        ServiceCollection services = CreateServices();

        services.AddGranitNotificationsZulip(
            configureBot: bot =>
            {
                bot.BaseUrl = "https://zulip.test.com";
                bot.BotEmail = "bot@test.com";
                bot.ApiKey = "key";
                bot.TimeoutSeconds = 45;
            });

        ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        HttpClient client = httpClientFactory.CreateClient(ZulipBotSender.HttpClientName);
        client.BaseAddress.ShouldNotBeNull();
        client.BaseAddress!.ToString().ShouldBe("https://zulip.test.com/");
    }

    [Fact]
    public void AddGranitNotificationsZulip_HttpClientBaseUrlTrailingSlashNormalized()
    {
        ServiceCollection services = CreateServices();

        services.AddGranitNotificationsZulip(
            configureBot: bot =>
            {
                bot.BaseUrl = "https://zulip.test.com/";
                bot.BotEmail = "bot@test.com";
                bot.ApiKey = "key";
            });

        ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        HttpClient client = httpClientFactory.CreateClient(ZulipBotSender.HttpClientName);
        client.BaseAddress.ShouldNotBeNull();
        client.BaseAddress!.ToString().ShouldBe("https://zulip.test.com/");
    }
}
