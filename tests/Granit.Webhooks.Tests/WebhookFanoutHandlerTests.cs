// =============================================================================
// Tests - WebhookFanoutHandler
// =============================================================================
// Verifies fan-out logic: empty result when no subscribers, correct command count
// and fields when subscribers exist, tenant resolution priority.
// =============================================================================

using System.Text.Json;
using Granit.Core.MultiTenancy;
using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Domain;
using Granit.Webhooks.Handlers;
using Granit.Webhooks.Messages;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Webhooks.Tests;

public sealed class WebhookFanoutHandlerTests
{
    private readonly IWebhookSubscriptionStore _store = Substitute.For<IWebhookSubscriptionStore>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly WebhookFanoutHandler _handler;

    public WebhookFanoutHandlerTests()
    {
        _currentTenant.IsAvailable.Returns(false);
        _handler = new WebhookFanoutHandler(_store, _currentTenant);
    }

    [Fact]
    public async Task HandleAsync_NoSubscribers_ReturnsEmptyEnumerable()
    {
        _store.GetActiveSubscriptionsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult<IReadOnlyList<WebhookSubscription>>([]));

        WebhookTrigger trigger = BuildTrigger();
        IEnumerable<SendWebhookCommand> result = await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ThreeSubscribers_ReturnsThreeCommands()
    {
        IReadOnlyList<WebhookSubscription> subscriptions = [
            BuildSubscription(), BuildSubscription(), BuildSubscription()
        ];
        _store.GetActiveSubscriptionsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(subscriptions));

        WebhookTrigger trigger = BuildTrigger();
        IEnumerable<SendWebhookCommand> result = await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        result.Count().ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_Commands_HaveDistinctDeliveryIds()
    {
        IReadOnlyList<WebhookSubscription> subscriptions = [BuildSubscription(), BuildSubscription()];
        _store.GetActiveSubscriptionsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(subscriptions));

        IEnumerable<SendWebhookCommand> result =
            await _handler.HandleAsync(BuildTrigger(), TestContext.Current.CancellationToken);

        var commands = result.ToList();
        commands.Select(c => c.DeliveryId).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_Commands_ShareSameEnvelopeEventId()
    {
        IReadOnlyList<WebhookSubscription> subscriptions = [BuildSubscription(), BuildSubscription()];
        _store.GetActiveSubscriptionsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(subscriptions));

        WebhookTrigger trigger = BuildTrigger();
        IEnumerable<SendWebhookCommand> result = await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        var commands = result.ToList();
        commands.Select(c => c.Envelope.EventId).Distinct().ShouldHaveSingleItem().ShouldBe(trigger.EventId);
    }

    [Fact]
    public async Task HandleAsync_UsesAmbientTenant_WhenAvailable()
    {
        var tenantId = Guid.NewGuid();
        _currentTenant.IsAvailable.Returns(true);
        _currentTenant.Id.Returns(tenantId);

        _store.GetActiveSubscriptionsAsync(Arg.Any<string>(), tenantId, Arg.Any<CancellationToken>())
              .Returns(Task.FromResult<IReadOnlyList<WebhookSubscription>>([]));

        WebhookTrigger trigger = BuildTrigger(tenantId: Guid.NewGuid()); // different from ambient
        await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        await _store.Received(1).GetActiveSubscriptionsAsync(
            Arg.Any<string>(), tenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FallsBackToTriggerTenantId_WhenAmbientNotAvailable()
    {
        var triggerTenantId = Guid.NewGuid();
        _currentTenant.IsAvailable.Returns(false);

        _store.GetActiveSubscriptionsAsync(Arg.Any<string>(), triggerTenantId, Arg.Any<CancellationToken>())
              .Returns(Task.FromResult<IReadOnlyList<WebhookSubscription>>([]));

        WebhookTrigger trigger = BuildTrigger(tenantId: triggerTenantId);
        await _handler.HandleAsync(trigger, TestContext.Current.CancellationToken);

        await _store.Received(1).GetActiveSubscriptionsAsync(
            Arg.Any<string>(), triggerTenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EnvelopeApiVersion_IsConstant()
    {
        IReadOnlyList<WebhookSubscription> subscriptions = [BuildSubscription()];
        _store.GetActiveSubscriptionsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(subscriptions));

        IEnumerable<SendWebhookCommand> result =
            await _handler.HandleAsync(BuildTrigger(), TestContext.Current.CancellationToken);

        result.Single().Envelope.ApiVersion.ShouldBe(WebhooksConstants.ApiVersion);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static WebhookTrigger BuildTrigger(Guid? tenantId = null) => new()
    {
        EventType = "test.event",
        Payload = JsonSerializer.SerializeToElement(new { key = "value" }),
        TenantId = tenantId,
        OccurredAt = DateTimeOffset.UtcNow,
    };

    private static WebhookSubscription BuildSubscription() => new()
    {
        Id = Guid.NewGuid(),
        TargetUrl = "https://example.com/webhook",
        EventType = "test.event",
        SigningSecret = "protected-secret",
        Status = WebhookSubscriptionStatus.Active,
    };
}
