using System.Net;
using System.Text.Json;
using Granit.Notifications.MobilePush;
using Granit.Notifications.MobilePush.Fcm;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Wolverine;
using Xunit;

namespace Granit.Notifications.MobilePush.Fcm.Tests;

public sealed class FcmMobilePushSenderTests
{
    [Fact]
    public async Task SendAsync_SuccessfulDelivery_DoesNotThrow()
    {
        FcmMobilePushSender sender = CreateSender(new DelegatingHandlerStub(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        MobilePushMessage message = BuildMessage("token-1");

        await sender.SendAsync(message, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendAsync_UnregisteredToken_PublishesInvalidationEvent()
    {
        IMessageBus messageBus = Substitute.For<IMessageBus>();
        FcmMobilePushSender sender = CreateSender(
            new DelegatingHandlerStub(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"error\":{\"code\":404,\"message\":\"UNREGISTERED\"}}"),
            }),
            messageBus);

        MobilePushMessage message = BuildMessage("expired-token");
        await sender.SendAsync(message, TestContext.Current.CancellationToken);

        await messageBus.Received(1).PublishAsync(Arg.Is<MobilePushTokenInvalidated>(e => e.DeviceToken == "expired-token"));
    }

    [Fact]
    public async Task SendAsync_ServerError_ThrowsAggregateException()
    {
        FcmMobilePushSender sender = CreateSender(
            new DelegatingHandlerStub(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"internal\"}"),
            }));

        MobilePushMessage message = BuildMessage("token-1");
        await Should.ThrowAsync<AggregateException>(() => sender.SendAsync(message, TestContext.Current.CancellationToken));
    }

    private static FcmMobilePushSender CreateSender(DelegatingHandler handler, IMessageBus? messageBus = null)
    {
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        HttpClient client = new(handler) { BaseAddress = new Uri("https://fcm.googleapis.com/") };
        factory.CreateClient("FcmPush").Returns(client);

        IOptions<FcmOptions> options = Options.Create(new FcmOptions { ProjectId = "test-project", ServiceAccountJson = "{}" });
        return new FcmMobilePushSender(factory, options, messageBus ?? Substitute.For<IMessageBus>(), NullLogger<FcmMobilePushSender>.Instance);
    }

    private static MobilePushMessage BuildMessage(params string[] tokens) => new()
    {
        DeviceTokens = tokens,
        Title = "Test",
        Body = "Test notification",
        Data = JsonSerializer.SerializeToElement(new { key = "value" }),
    };

    private sealed class DelegatingHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> handler) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
