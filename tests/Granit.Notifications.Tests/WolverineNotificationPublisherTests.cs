// =============================================================================
// Tests - WolverineNotificationPublisher
// =============================================================================
// Verifies Wolverine-backed publisher: trigger construction with explicit
// recipients, entity references, subscriber/follower variants, tenant capture.
// =============================================================================

using Granit.Core.MultiTenancy;
using Granit.Notifications.Internal;
using Granit.Notifications.Messages;
using Granit.Timing;
using NSubstitute;
using Shouldly;
using Wolverine;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class WolverineNotificationPublisherTests
{
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly IClock _clock;
    private readonly WolverineNotificationPublisher _publisher;

    public WolverineNotificationPublisherTests()
    {
        _clock = Substitute.For<IClock>();
        _clock.Now.Returns(_ => DateTimeOffset.UtcNow);
        _currentTenant.IsAvailable.Returns(false);
        _publisher = new WolverineNotificationPublisher(_bus, _currentTenant, _clock);
    }

    [Fact]
    public async Task PublishAsync_ExplicitRecipients_PublishesTriggerWithRecipients()
    {
        IReadOnlyList<string> recipients = ["user-1", "user-2"];

        await _publisher.PublishAsync(
            TestNotificationType.Instance,
            new TestPayload("value"),
            recipients,
            TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<NotificationTrigger>(t =>
                t.NotificationTypeName == "test.notification" &&
                t.RecipientUserIds.Count == 2));
    }

    [Fact]
    public async Task PublishAsync_WithEntityReference_IncludesRelatedEntity()
    {
        EntityReference entity = new("Invoice", "inv-42");

        await _publisher.PublishAsync(
            TestNotificationType.Instance,
            new TestPayload("value"),
            ["user-1"],
            entity,
            TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<NotificationTrigger>(t =>
                t.RelatedEntity != null &&
                t.RelatedEntity.EntityType == "Invoice" &&
                t.RelatedEntity.EntityId == "inv-42"));
    }

    [Fact]
    public async Task PublishToSubscribersAsync_PublishesTriggerWithNoRecipients()
    {
        await _publisher.PublishToSubscribersAsync(
            TestNotificationType.Instance,
            new TestPayload("value"),
            TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<NotificationTrigger>(t =>
                t.NotificationTypeName == "test.notification" &&
                t.RecipientUserIds.Count == 0 &&
                t.RelatedEntity == null));
    }

    [Fact]
    public async Task PublishToEntityFollowersAsync_PublishesTriggerWithEntityReference()
    {
        EntityReference entity = new("Document", "doc-99");

        await _publisher.PublishToEntityFollowersAsync(
            TestNotificationType.Instance,
            new TestPayload("value"),
            entity,
            TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<NotificationTrigger>(t =>
                t.RecipientUserIds.Count == 0 &&
                t.RelatedEntity != null &&
                t.RelatedEntity.EntityType == "Document" &&
                t.RelatedEntity.EntityId == "doc-99"));
    }

    [Fact]
    public async Task PublishAsync_CapturesAmbientTenantId()
    {
        var tenantId = Guid.NewGuid();
        _currentTenant.IsAvailable.Returns(true);
        _currentTenant.Id.Returns(tenantId);

        await _publisher.PublishAsync(
            TestNotificationType.Instance,
            new TestPayload("value"),
            ["user-1"],
            TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<NotificationTrigger>(t => t.TenantId == tenantId));
    }

    [Fact]
    public async Task PublishAsync_NoTenant_TenantIdIsNull()
    {
        _currentTenant.IsAvailable.Returns(false);

        await _publisher.PublishAsync(
            TestNotificationType.Instance,
            new TestPayload("value"),
            ["user-1"],
            TestContext.Current.CancellationToken);

        await _bus.Received(1).PublishAsync(
            Arg.Is<NotificationTrigger>(t => t.TenantId == null));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private sealed record TestPayload(string Key);

    private sealed class TestNotificationType : NotificationType<TestPayload>
    {
        public static readonly TestNotificationType Instance = new();
        public override string Name => "test.notification";
        public override IReadOnlyList<string> DefaultChannels => [NotificationChannels.InApp];
    }
}
