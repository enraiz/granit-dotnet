using System.Net;
using Granit.Notifications.Brevo.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Brevo.Tests;

public sealed class BrevoHealthCheckTests
{
    private static HealthCheckContext CreateContext() =>
        new() { Registration = new HealthCheckRegistration("test", _ => null!, null, null) };

    [Fact]
    public async Task CheckHealthAsync_SuccessfulResponse_ReturnsHealthy()
    {
        // Arrange
        FakeHandler handler = CreateHandler(HttpStatusCode.OK);
        IHttpClientFactory factory = CreateFactory(handler);
        BrevoHealthCheck sut = new(factory);

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
        FakeHandler handler = CreateHandler(HttpStatusCode.Unauthorized);
        IHttpClientFactory factory = CreateFactory(handler);
        BrevoHealthCheck sut = new(factory);

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
        FakeHandler handler = CreateHandler(HttpStatusCode.InternalServerError);
        IHttpClientFactory factory = CreateFactory(handler);
        BrevoHealthCheck sut = new(factory);

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
        factory.CreateClient("Brevo")
            .Returns(_ => throw new HttpRequestException("Connection refused to api.brevo.com with apikey=abc123"));

        BrevoHealthCheck sut = new(factory);

        // Act
        HealthCheckResult result = await sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Brevo unreachable: HttpRequestException");
        result.Description!.ShouldNotContain("api.brevo.com");
        result.Description!.ShouldNotContain("apikey");
    }

    private static FakeHandler CreateHandler(HttpStatusCode statusCode) =>
        new(statusCode);

    private static IHttpClientFactory CreateFactory(HttpMessageHandler handler)
    {
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Brevo").Returns(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.brevo.com/v3/"),
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
