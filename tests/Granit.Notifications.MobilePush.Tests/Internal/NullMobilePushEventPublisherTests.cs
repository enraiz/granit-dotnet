using Granit.Notifications.MobilePush.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Notifications.MobilePush.Tests.Internal;

public sealed class NullMobilePushEventPublisherTests
{
    private readonly ILogger<NullMobilePushEventPublisher> _logger =
        Substitute.For<ILogger<NullMobilePushEventPublisher>>();

    private readonly NullMobilePushEventPublisher _publisher;

    public NullMobilePushEventPublisherTests() =>
        _publisher = new NullMobilePushEventPublisher(_logger);

    [Fact]
    public async Task PublishTokenInvalidatedAsync_CompletesWithoutThrowing()
    {
        MobilePushTokenInvalidated tokenInvalidated = new()
        {
            DeviceToken = "expired-token-abc",
        };

        Func<Task> act = () => _publisher.PublishTokenInvalidatedAsync(
            tokenInvalidated, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task PublishTokenInvalidatedAsync_ReturnsCompletedTask()
    {
        MobilePushTokenInvalidated tokenInvalidated = new()
        {
            DeviceToken = "test-token",
        };

        Task result = _publisher.PublishTokenInvalidatedAsync(
            tokenInvalidated, TestContext.Current.CancellationToken);

        result.IsCompleted.ShouldBeTrue();
        await result;
    }

    [Fact]
    public async Task PublishTokenInvalidatedAsync_WithTenantId_CompletesWithoutThrowing()
    {
        MobilePushTokenInvalidated tokenInvalidated = new()
        {
            DeviceToken = "device-token-123",
            TenantId = Guid.NewGuid(),
        };

        Func<Task> act = () => _publisher.PublishTokenInvalidatedAsync(
            tokenInvalidated, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public void ImplementsIMobilePushEventPublisher() =>
        _publisher.ShouldBeAssignableTo<IMobilePushEventPublisher>();
}
