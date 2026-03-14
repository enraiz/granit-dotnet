using System.Net;
using Granit.Notifications.Zulip.HealthChecks;
using Granit.Notifications.Zulip.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Zulip.Tests;

public sealed class ZulipHealthCheckTests
{
    private readonly ZulipBotOptions _options = new()
    {
        BaseUrl = "https://zulip.example.com",
        BotEmail = "bot@zulip.example.com",
        ApiKey = "test-api-key",
        TimeoutSeconds = 30,
    };

    private static HealthCheckContext CreateContext() =>
        new() { Registration = new HealthCheckRegistration("test", _ => null!, null, null) };

    [Fact]
    public async Task CheckHealthAsync_SuccessfulResponse_ReturnsHealthy()
    {
        // Arrange
        FakeHandler handler = new(HttpStatusCode.OK);
        IHttpClientFactory factory = CreateFactory(handler);
        ZulipHealthCheck sut = new(factory, Microsoft.Extensions.Options.Options.Create(_options));

        // Act
        HealthCheckResult result = await sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_Unauthorized_ReturnsUnhealthy()
    {
        // Arrange
        FakeHandler handler = new(HttpStatusCode.Unauthorized);
        IHttpClientFactory factory = CreateFactory(handler);
        ZulipHealthCheck sut = new(factory, Microsoft.Extensions.Options.Options.Create(_options));

        // Act
        HealthCheckResult result = await sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("401");
    }

    [Fact]
    public async Task CheckHealthAsync_ServerError_ReturnsDegraded()
    {
        // Arrange
        FakeHandler handler = new(HttpStatusCode.InternalServerError);
        IHttpClientFactory factory = CreateFactory(handler);
        ZulipHealthCheck sut = new(factory, Microsoft.Extensions.Options.Options.Create(_options));

        // Act
        HealthCheckResult result = await sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("500");
    }

    [Fact]
    public async Task CheckHealthAsync_Exception_ReturnsUnhealthyWithSanitizedMessage()
    {
        // Arrange
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("ZulipBot")
            .Returns(_ => throw new HttpRequestException("Connection refused to zulip.example.com with key=secret123"));

        ZulipHealthCheck sut = new(factory, Microsoft.Extensions.Options.Options.Create(_options));

        // Act
        HealthCheckResult result = await sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Zulip unreachable: HttpRequestException");
        result.Description!.ShouldNotContain("zulip.example.com");
        result.Description!.ShouldNotContain("secret");
    }

    private static IHttpClientFactory CreateFactory(HttpMessageHandler handler)
    {
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("ZulipBot").Returns(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://zulip.example.com/"),
        });
        return factory;
    }

    private sealed class FakeHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
