// =============================================================================
// Tests - SendWebhookHandler
// =============================================================================
// Verifies HTTP delivery: success path, non-retriable errors (suspension),
// retriable errors (exception thrown), network timeout handling.
// =============================================================================

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Exceptions;
using Granit.Webhooks.Handlers;
using Granit.Webhooks.Internal;
using Granit.Webhooks.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Granit.Webhooks.Tests;

public sealed class SendWebhookHandlerTests
{
    private readonly IWebhookDeliveryStore _deliveryStore = Substitute.For<IWebhookDeliveryStore>();

    // Use the real no-op protector to avoid CA2012 when mocking ValueTask-returning methods.
    private readonly IWebhookSecretProtector _secretProtector = new NoOpWebhookSecretProtector();

    // -------------------------------------------------------------------------
    // Success
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_Http2xx_CallsRecordSuccess()
    {
        SendWebhookHandler handler = BuildHandler(HttpStatusCode.OK);
        SendWebhookCommand command = BuildCommand();

        await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await _deliveryStore.Received(1).RecordSuccessAsync(
            command, 200, Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Http201_DoesNotThrow()
    {
        SendWebhookHandler handler = BuildHandler(HttpStatusCode.Created);

        Func<Task> act = () => handler.HandleAsync(BuildCommand(), TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------------
    // Non-retriable: return without throw, record failure
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.MethodNotAllowed)]
    public async Task HandleAsync_NonRetriableNonSuspending_RecordsFailureAndReturns(HttpStatusCode statusCode)
    {
        SendWebhookHandler handler = BuildHandler(statusCode);
        SendWebhookCommand command = BuildCommand();

        Func<Task> act = () => handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _deliveryStore.Received(1).RecordFailureAsync(
            command, (int)statusCode, Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _deliveryStore.DidNotReceive().SuspendSubscriptionAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    public async Task HandleAsync_NonRetriableSuspending_SuspendsSubscription(HttpStatusCode statusCode)
    {
        SendWebhookHandler handler = BuildHandler(statusCode);
        SendWebhookCommand command = BuildCommand();

        await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await _deliveryStore.Received(1).SuspendSubscriptionAsync(
            command.SubscriptionId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Retriable: throw WebhookDeliveryException
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task HandleAsync_RetriableHttpError_ThrowsWebhookDeliveryException(HttpStatusCode statusCode)
    {
        SendWebhookHandler handler = BuildHandler(statusCode);

        Func<Task> act = () => handler.HandleAsync(BuildCommand(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WebhookDeliveryException>();
    }

    [Fact]
    public async Task HandleAsync_RetriableError_RecordsFailureBeforeThrow()
    {
        SendWebhookHandler handler = BuildHandler(HttpStatusCode.ServiceUnavailable);
        SendWebhookCommand command = BuildCommand();

        await Assert.ThrowsAsync<WebhookDeliveryException>(
            () => handler.HandleAsync(command, TestContext.Current.CancellationToken));

        await _deliveryStore.Received(1).RecordFailureAsync(
            command, 503, Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Timeout
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_NetworkTimeout_ThrowsWebhookDeliveryException()
    {
        SendWebhookHandler handler = BuildHandlerWithTimeout();
        SendWebhookCommand command = BuildCommand();

        Func<Task> act = () => handler.HandleAsync(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WebhookDeliveryException>()
            .WithMessage("*Timeout*");
    }

    [Fact]
    public async Task HandleAsync_NetworkTimeout_RecordsFailureWithNullStatusCode()
    {
        SendWebhookHandler handler = BuildHandlerWithTimeout();
        SendWebhookCommand command = BuildCommand();

        await Assert.ThrowsAsync<WebhookDeliveryException>(
            () => handler.HandleAsync(command, TestContext.Current.CancellationToken));

        await _deliveryStore.Received(1).RecordFailureAsync(
            command, null, Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private SendWebhookHandler BuildHandler(HttpStatusCode statusCode)
    {
        HttpClient httpClient = new(new StaticResponseHandler(statusCode));
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        return new SendWebhookHandler(factory, _deliveryStore, _secretProtector, NullLogger<SendWebhookHandler>.Instance);
    }

    private SendWebhookHandler BuildHandlerWithTimeout()
    {
        HttpClient httpClient = new(new TimeoutHandler());
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        return new SendWebhookHandler(factory, _deliveryStore, _secretProtector, NullLogger<SendWebhookHandler>.Instance);
    }

    private static SendWebhookCommand BuildCommand() => new()
    {
        DeliveryId = Guid.NewGuid(),
        SubscriptionId = Guid.NewGuid(),
        TargetUrl = "https://example.com/webhook",
        SigningSecret = "test-secret",
        Envelope = new WebhookEnvelope
        {
            EventId = Guid.NewGuid(),
            EventType = "test.event",
            TenantId = null,
            Timestamp = DateTimeOffset.UtcNow,
            ApiVersion = "2025-01-01",
            Data = JsonSerializer.SerializeToElement(new { key = "value" }),
        },
    };

    // -------------------------------------------------------------------------
    // Test doubles
    // -------------------------------------------------------------------------

    private sealed class StaticResponseHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new TaskCanceledException("Simulated network timeout");
    }
}
