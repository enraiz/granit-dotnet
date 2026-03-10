// =============================================================================
// Tests - WolverineWebhookPublisher
// =============================================================================
// Verifies that PublishAsync serializes the payload and publishes a WebhookTrigger
// with correct fields, including tenant context handling.
// =============================================================================

using System.Text.Json;
using Granit.Core.MultiTenancy;
using Granit.Timing;
using Granit.Webhooks.Internal;
using Granit.Webhooks.Messages;
using NSubstitute;
using Shouldly;
using Wolverine;
using Xunit;

namespace Granit.Webhooks.Tests;

public sealed class WolverineWebhookPublisherTests
{
    private readonly IMessageBus _messageBus = Substitute.For<IMessageBus>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DateTimeOffset _fixedNow = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    public WolverineWebhookPublisherTests()
    {
        _clock.Now.Returns(_ => _fixedNow);
    }

    [Fact]
    public async Task PublishAsync_WithTenant_PublishesTriggerWithTenantId()
    {
        var tenantId = Guid.NewGuid();
        _currentTenant.IsAvailable.Returns(true);
        _currentTenant.Id.Returns(tenantId);

        var publisher = new WolverineWebhookPublisher(_messageBus, _currentTenant, _clock);
        var payload = new { DocumentId = Guid.NewGuid(), Name = "test.pdf" };

        await publisher.PublishAsync("document.uploaded", payload, TestContext.Current.CancellationToken);

        await _messageBus.Received(1).PublishAsync(Arg.Is<WebhookTrigger>(t =>
            t.EventType == "document.uploaded"
            && t.TenantId == tenantId
            && t.OccurredAt == _fixedNow));
    }

    [Fact]
    public async Task PublishAsync_WithoutTenant_PublishesTriggerWithNullTenantId()
    {
        _currentTenant.IsAvailable.Returns(false);

        var publisher = new WolverineWebhookPublisher(_messageBus, _currentTenant, _clock);

        await publisher.PublishAsync("test.event", new { Key = "value" }, TestContext.Current.CancellationToken);

        await _messageBus.Received(1).PublishAsync(Arg.Is<WebhookTrigger>(t =>
            t.TenantId == null));
    }

    [Fact]
    public async Task PublishAsync_SerializesPayloadToJsonElement()
    {
        _currentTenant.IsAvailable.Returns(false);

        var publisher = new WolverineWebhookPublisher(_messageBus, _currentTenant, _clock);
        var payload = new { Id = 42, Label = "test" };

        await publisher.PublishAsync("item.created", payload, TestContext.Current.CancellationToken);

        await _messageBus.Received(1).PublishAsync(Arg.Is<WebhookTrigger>(t =>
            t.Payload.GetProperty("Id").GetInt32() == 42
            && t.Payload.GetProperty("Label").GetString() == "test"));
    }

    [Fact]
    public async Task PublishAsync_SetsOccurredAtFromClock()
    {
        _currentTenant.IsAvailable.Returns(false);

        var publisher = new WolverineWebhookPublisher(_messageBus, _currentTenant, _clock);

        await publisher.PublishAsync("test.event", new { }, TestContext.Current.CancellationToken);

        await _messageBus.Received(1).PublishAsync(Arg.Is<WebhookTrigger>(t =>
            t.OccurredAt == _fixedNow));
    }
}
