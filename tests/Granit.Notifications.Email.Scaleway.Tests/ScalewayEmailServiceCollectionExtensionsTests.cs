using Granit.Notifications.Email.Scaleway.Extensions;
using Granit.Notifications.Email.Scaleway.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Email.Scaleway.Tests;

public sealed class ScalewayEmailServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitNotificationsEmailScaleway_RegistersKeyedEmailSender()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsEmailScaleway();

        services.ShouldContain(d =>
            d.IsKeyedService &&
            d.ServiceType == typeof(IEmailSender) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitNotificationsEmailScaleway_AppliesConfigureDelegate()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddGranitNotificationsEmailScaleway(opts =>
        {
            opts.SecretKey = "scw-test-key";
            opts.ProjectId = "project-abc";
            opts.DefaultSenderEmail = "test@example.com";
            opts.DefaultSenderName = "Test";
        });

        ServiceProvider sp = services.BuildServiceProvider();
        ScalewayEmailOptions options = sp.GetRequiredService<IOptions<ScalewayEmailOptions>>().Value;

        options.SecretKey.ShouldBe("scw-test-key");
        options.ProjectId.ShouldBe("project-abc");
        options.DefaultSenderEmail.ShouldBe("test@example.com");
        options.DefaultSenderName.ShouldBe("Test");
    }

    [Fact]
    public void AddGranitNotificationsEmailScaleway_ReturnsServiceCollection()
    {
        ServiceCollection services = new();
        IServiceCollection result = services.AddGranitNotificationsEmailScaleway();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitNotificationsEmailScaleway_WithNullConfigure_DoesNotThrow()
    {
        ServiceCollection services = new();

        Should.NotThrow(() => services.AddGranitNotificationsEmailScaleway(configure: null));
    }

    [Fact]
    public void AddGranitNotificationsEmailScaleway_RegistersHttpClient()
    {
        ServiceCollection services = new();
        services.AddGranitNotificationsEmailScaleway();

        services.ShouldContain(d =>
            d.ServiceType == typeof(IHttpClientFactory));
    }

    [Fact]
    public void AddGranitNotificationsEmailScaleway_SetsXAuthTokenHeader()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:Email:Scaleway:SecretKey"] = "my-secret",
                ["Notifications:Email:Scaleway:ProjectId"] = "proj-1",
                ["Notifications:Email:Scaleway:DefaultSenderEmail"] = "a@b.com",
            })
            .Build());
        services.AddGranitNotificationsEmailScaleway();

        ServiceProvider sp = services.BuildServiceProvider();
        IHttpClientFactory factory = sp.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = factory.CreateClient("Scaleway");

        client.DefaultRequestHeaders.TryGetValues("X-Auth-Token", out IEnumerable<string>? values).ShouldBeTrue();
        values!.ShouldContain("my-secret");
    }
}
