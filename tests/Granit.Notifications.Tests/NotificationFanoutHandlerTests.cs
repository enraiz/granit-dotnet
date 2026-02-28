// =============================================================================
// Tests - NotificationFanoutHandler
// =============================================================================
// Verifies fan-out logic: empty result when no recipients/subscribers, correct
// command count per recipient x channel, opt-out filtering, entity followers
// fallback, tenant resolution priority, and distinct delivery IDs.
// =============================================================================

using System.Text.Json;
using FluentAssertions;
using Granit.Core.MultiTenancy;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Handlers;
using Granit.Notifications.Messages;
using NSubstitute;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class NotificationFanoutHandlerTests
{
    private readonly INotificationSubscriptionStore _subscriptionStore = Substitute.For<INotificationSubscriptionStore>();
    private readonly INotificationPreferenceStore _preferenceStore = Substitute.For<INotificationPreferenceStore>();
    private readonly INotificationDefinitionStore _definitionStore = Substitute.For<INotificationDefinitionStore>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly NotificationFanoutHandler _handler;

    public NotificationFanoutHandlerTests()
    {
        _currentTenant.IsAvailable.Returns(false);
        _handler = new NotificationFanoutHandler(
            _subscriptionStore,
            _preferenceStore,
            _definitionStore,
            _currentTenant);
    }

    [Fact]
    public async Task HandleAsync_NoRecipients_NoSubscribers_ReturnsEmpty()
    {
        NotificationDefinition definition = BuildDefinition("test.notification", [NotificationChannels.InApp]);
        _definitionStore.Get("test.notification").Returns(definition);
        _subscriptionStore.GetSubscriberIdsAsync("test.notification", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([]));

        NotificationTrigger trigger = BuildTrigger(recipientUserIds: []);

        IEnumerable<DeliverNotificationCommand> result =
            await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ExplicitRecipients_TwoUsersThreeChannels_ReturnsSixCommands()
    {
        NotificationDefinition definition = BuildDefinition("test.notification",
            [NotificationChannels.InApp, NotificationChannels.Email, NotificationChannels.Push]);
        _definitionStore.Get("test.notification").Returns(definition);
        _preferenceStore.IsChannelEnabledAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        NotificationTrigger trigger = BuildTrigger(recipientUserIds: ["user-1", "user-2"]);

        IEnumerable<DeliverNotificationCommand> result =
            await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        result.Should().HaveCount(6);
    }

    [Fact]
    public async Task HandleAsync_OptedOutChannel_SkipsCommand()
    {
        NotificationDefinition definition = BuildDefinition("test.notification",
            [NotificationChannels.InApp, NotificationChannels.Email]);
        _definitionStore.Get("test.notification").Returns(definition);

        _preferenceStore.IsChannelEnabledAsync("user-1", "test.notification", NotificationChannels.InApp, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _preferenceStore.IsChannelEnabledAsync("user-1", "test.notification", NotificationChannels.Email, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        NotificationTrigger trigger = BuildTrigger(recipientUserIds: ["user-1"]);

        IEnumerable<DeliverNotificationCommand> result =
            await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        List<DeliverNotificationCommand> commands = result.ToList();
        commands.Should().HaveCount(1);
        commands.Single().ChannelName.Should().Be(NotificationChannels.InApp);
    }

    [Fact]
    public async Task HandleAsync_AllowUserOptOutFalse_IgnoresPreference()
    {
        NotificationDefinition definition = BuildDefinition("security.alert",
            [NotificationChannels.InApp, NotificationChannels.Email], allowUserOptOut: false);
        _definitionStore.Get("security.alert").Returns(definition);

        NotificationTrigger trigger = BuildTrigger(notificationTypeName: "security.alert", recipientUserIds: ["user-1"]);

        IEnumerable<DeliverNotificationCommand> result =
            await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        result.Should().HaveCount(2, "AllowUserOptOut is false, so opt-out is ignored");
    }

    [Fact]
    public async Task HandleAsync_NoExplicitRecipients_WithEntityFollowers_UsesFollowers()
    {
        NotificationDefinition definition = BuildDefinition("test.notification", [NotificationChannels.InApp]);
        _definitionStore.Get("test.notification").Returns(definition);
        _preferenceStore.IsChannelEnabledAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        EntityReference entity = new("Invoice", "inv-42");
        _subscriptionStore.GetEntityFollowerIdsAsync(entity.EntityType, entity.EntityId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(["follower-1", "follower-2"]));

        NotificationTrigger trigger = BuildTrigger(recipientUserIds: [], relatedEntity: entity);

        IEnumerable<DeliverNotificationCommand> result =
            await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        await _subscriptionStore.Received(1).GetEntityFollowerIdsAsync(
            entity.EntityType, entity.EntityId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoExplicitRecipients_NoFollowers_UsesSubscribers()
    {
        NotificationDefinition definition = BuildDefinition("test.notification", [NotificationChannels.InApp]);
        _definitionStore.Get("test.notification").Returns(definition);
        _preferenceStore.IsChannelEnabledAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        _subscriptionStore.GetSubscriberIdsAsync("test.notification", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(["subscriber-1"]));

        NotificationTrigger trigger = BuildTrigger(recipientUserIds: []);

        IEnumerable<DeliverNotificationCommand> result =
            await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        await _subscriptionStore.Received(1).GetSubscriberIdsAsync(
            "test.notification", Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UsesAmbientTenant_WhenAvailable()
    {
        Guid tenantId = Guid.NewGuid();
        _currentTenant.IsAvailable.Returns(true);
        _currentTenant.Id.Returns(tenantId);

        NotificationDefinition definition = BuildDefinition("test.notification", [NotificationChannels.InApp]);
        _definitionStore.Get("test.notification").Returns(definition);
        _preferenceStore.IsChannelEnabledAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        _subscriptionStore.GetSubscriberIdsAsync("test.notification", tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(["user-1"]));

        NotificationTrigger trigger = BuildTrigger(recipientUserIds: [], tenantId: Guid.NewGuid());

        await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        await _subscriptionStore.Received(1).GetSubscriberIdsAsync(
            Arg.Any<string>(), tenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FallsBackToTriggerTenantId_WhenAmbientNotAvailable()
    {
        Guid triggerTenantId = Guid.NewGuid();
        _currentTenant.IsAvailable.Returns(false);

        NotificationDefinition definition = BuildDefinition("test.notification", [NotificationChannels.InApp]);
        _definitionStore.Get("test.notification").Returns(definition);
        _preferenceStore.IsChannelEnabledAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        _subscriptionStore.GetSubscriberIdsAsync("test.notification", triggerTenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(["user-1"]));

        NotificationTrigger trigger = BuildTrigger(recipientUserIds: [], tenantId: triggerTenantId);

        await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        await _subscriptionStore.Received(1).GetSubscriberIdsAsync(
            Arg.Any<string>(), triggerTenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Commands_HaveDistinctDeliveryIds()
    {
        NotificationDefinition definition = BuildDefinition("test.notification",
            [NotificationChannels.InApp, NotificationChannels.Email]);
        _definitionStore.Get("test.notification").Returns(definition);
        _preferenceStore.IsChannelEnabledAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        NotificationTrigger trigger = BuildTrigger(recipientUserIds: ["user-1", "user-2"]);

        IEnumerable<DeliverNotificationCommand> result =
            await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        List<DeliverNotificationCommand> commands = result.ToList();
        commands.Select(c => c.DeliveryId).Distinct().Should().HaveCount(commands.Count);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static NotificationTrigger BuildTrigger(
        string notificationTypeName = "test.notification",
        IReadOnlyList<string>? recipientUserIds = null,
        EntityReference? relatedEntity = null,
        Guid? tenantId = null) => new()
        {
            NotificationTypeName = notificationTypeName,
            Severity = NotificationSeverity.Info,
            Data = JsonSerializer.SerializeToElement(new { key = "value" }),
            RecipientUserIds = recipientUserIds ?? ["user-1"],
            RelatedEntity = relatedEntity,
            TenantId = tenantId,
            OccurredAt = DateTimeOffset.UtcNow,
        };

    private static NotificationDefinition BuildDefinition(
        string name,
        IReadOnlyList<string> channels,
        bool allowUserOptOut = true) => new(name)
        {
            DefaultChannels = channels,
            AllowUserOptOut = allowUserOptOut,
        };
}
