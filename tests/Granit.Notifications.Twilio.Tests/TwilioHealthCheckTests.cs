using System.Net;
using Granit.Notifications.Twilio.HealthChecks;
using Granit.Notifications.Twilio.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Twilio.Tests;

public sealed class TwilioHealthCheckTests
{
    private static HealthCheckContext CreateContext() =>
        new() { Registration = new HealthCheckRegistration("test", _ => null!, null, null) };

    private static IOptionsMonitor<TwilioOptions> CreateOptionsMonitor()
    {
        IOptionsMonitor<TwilioOptions> monitor = Substitute.For<IOptionsMonitor<TwilioOptions>>();
        monitor.CurrentValue.Returns(new TwilioOptions
        {
            AccountSid = "AC_test_sid",
            AuthToken = "test-token",
            DefaultSmsFromNumber = "+15551234567",
        });
        return monitor;
    }

    [Fact]
    public async Task CheckHealthAsync_SuccessfulResponse_ReturnsHealthy()
    {
        FakeHandler handler = CreateHandler(HttpStatusCode.OK);
        IHttpClientFactory factory = CreateFactory(handler);
        TwilioHealthCheck sut = new(factory, CreateOptionsMonitor());

        HealthCheckResult result = await sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_Unauthorized_ReturnsUnhealthy()
    {
        FakeHandler handler = CreateHandler(HttpStatusCode.Unauthorized);
        IHttpClientFactory factory = CreateFactory(handler);
        TwilioHealthCheck sut = new(factory, CreateOptionsMonitor());

        HealthCheckResult result = await sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("401");
    }

    [Fact]
    public async Task CheckHealthAsync_ServerError_ReturnsDegraded()
    {
        FakeHandler handler = CreateHandler(HttpStatusCode.InternalServerError);
        IHttpClientFactory factory = CreateFactory(handler);
        TwilioHealthCheck sut = new(factory, CreateOptionsMonitor());

        HealthCheckResult result = await sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("500");
    }

    [Fact]
    public async Task CheckHealthAsync_Exception_ReturnsUnhealthyWithSanitizedMessage()
    {
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Twilio")
            .Returns(_ => throw new HttpRequestException("Connection refused to api.twilio.com with auth=secret123"));

        TwilioHealthCheck sut = new(factory, CreateOptionsMonitor());

        HealthCheckResult result = await sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Twilio unreachable: HttpRequestException");
        result.Description!.ShouldNotContain("api.twilio.com");
        result.Description!.ShouldNotContain("secret123");
    }

    [Fact]
    public async Task CheckHealthAsync_UsesCorrectAccountEndpoint()
    {
        FakeHandler handler = CreateHandler(HttpStatusCode.OK);
        IHttpClientFactory factory = CreateFactory(handler);
        TwilioHealthCheck sut = new(factory, CreateOptionsMonitor());

        await sut.CheckHealthAsync(CreateContext(), TestContext.Current.CancellationToken);

        handler.LastRequestUri.ShouldNotBeNull();
        handler.LastRequestUri!.PathAndQuery.ShouldContain("2010-04-01/Accounts/AC_test_sid.json");
    }

    private static FakeHandler CreateHandler(HttpStatusCode statusCode) =>
        new(statusCode);

    private static IHttpClientFactory CreateFactory(HttpMessageHandler handler)
    {
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Twilio").Returns(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.twilio.com/"),
        });
        return factory;
    }

    private sealed class FakeHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
