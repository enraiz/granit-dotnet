using Granit.Guids;
using Granit.Timing;
using Granit.Webhooks.Domain;
using Granit.Webhooks.EntityFrameworkCore.Internal;
using Granit.Webhooks.Messages;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Webhooks.EntityFrameworkCore.Tests;

public sealed class EfWebhookDeliveryStoreTests : IAsyncDisposable
{
    private readonly DateTimeOffset _now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly IClock _clock;
    private readonly IDbContextFactory<WebhooksDbContext> _contextFactory;
    private readonly DbContextOptions<WebhooksDbContext> _options;
    private readonly EfWebhookDeliveryStore _sut;

    public EfWebhookDeliveryStoreTests()
    {
        _clock = Substitute.For<IClock>();
        _clock.Now.Returns(_ => _now);

        _options = new DbContextOptionsBuilder<WebhooksDbContext>()
            .UseInMemoryDatabase(databaseName: $"webhooks-delivery-{Guid.NewGuid()}")
            .Options;

        _contextFactory = new TestWebhooksDbContextFactory(_options);
        _sut = new EfWebhookDeliveryStore(_contextFactory, _clock, new SimpleGuidGenerator());
    }

    public async ValueTask DisposeAsync()
    {
        await using WebhooksDbContext context = new(_options);
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task RecordSuccessAsync_creates_delivery_attempt()
    {
        // Arrange
        SendWebhookCommand command = BuildCommand();

        // Act
        await _sut.RecordSuccessAsync(command, 200, 42, "abc123", null, TestContext.Current.CancellationToken);

        // Assert
        await using WebhooksDbContext context = new(_options);
        WebhookDeliveryAttempt attempt = await context.WebhookDeliveryAttempts.SingleAsync(TestContext.Current.CancellationToken);
        attempt.DeliveryId.ShouldBe(command.DeliveryId);
        attempt.SubscriptionId.ShouldBe(command.SubscriptionId);
        attempt.HttpStatusCode.ShouldBe(200);
        attempt.DurationMs.ShouldBe(42);
        attempt.PayloadHash.ShouldBe("abc123");
        attempt.IsSuccess.ShouldBeTrue();
        attempt.ErrorMessage.ShouldBeNull();
        attempt.OccurredAt.ShouldBe(_now);
    }

    [Fact]
    public async Task RecordSuccessAsync_resets_subscription_failure_count()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        await SeedSubscriptionAsync(subscriptionId, consecutiveFailures: 5);
        SendWebhookCommand command = BuildCommand(subscriptionId: subscriptionId);

        // Act
        await _sut.RecordSuccessAsync(command, 200, 10, "hash", null, TestContext.Current.CancellationToken);

        // Assert
        await using WebhooksDbContext context = new(_options);
        WebhookSubscription? subscription = await context.WebhookSubscriptions.FindAsync([subscriptionId], TestContext.Current.CancellationToken);
        subscription.ShouldNotBeNull();
        subscription!.ConsecutiveFailureCount.ShouldBe(0);
        subscription.LastSuccessAt.ShouldBe(_now);
    }

    [Fact]
    public async Task RecordSuccessAsync_no_op_when_subscription_not_found()
    {
        // Arrange — no subscription seeded
        SendWebhookCommand command = BuildCommand();

        // Act & Assert — should not throw
        await Should.NotThrowAsync(async () =>
            await _sut.RecordSuccessAsync(command, 200, 10, "hash", null, TestContext.Current.CancellationToken));

        await using WebhooksDbContext context = new(_options);
        (await context.WebhookDeliveryAttempts.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task RecordFailureAsync_creates_failure_attempt()
    {
        // Arrange
        SendWebhookCommand command = BuildCommand();

        // Act
        await _sut.RecordFailureAsync(command, 500, 100, "Server Error", null, TestContext.Current.CancellationToken);

        // Assert
        await using WebhooksDbContext context = new(_options);
        WebhookDeliveryAttempt attempt = await context.WebhookDeliveryAttempts.SingleAsync(TestContext.Current.CancellationToken);
        attempt.IsSuccess.ShouldBeFalse();
        attempt.HttpStatusCode.ShouldBe(500);
        attempt.ErrorMessage.ShouldBe("Server Error");
        attempt.PayloadHash.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task RecordFailureAsync_increments_consecutive_failure_count()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        await SeedSubscriptionAsync(subscriptionId, consecutiveFailures: 2);
        SendWebhookCommand command = BuildCommand(subscriptionId: subscriptionId);

        // Act
        await _sut.RecordFailureAsync(command, 500, 50, "Error", null, TestContext.Current.CancellationToken);

        // Assert
        await using WebhooksDbContext context = new(_options);
        WebhookSubscription? subscription = await context.WebhookSubscriptions.FindAsync([subscriptionId], TestContext.Current.CancellationToken);
        subscription!.ConsecutiveFailureCount.ShouldBe(3);
    }

    [Fact]
    public async Task RecordFailureAsync_truncates_long_error_message()
    {
        // Arrange
        SendWebhookCommand command = BuildCommand();
        string longError = new('X', 3000);

        // Act
        await _sut.RecordFailureAsync(command, null, 50, longError, null, TestContext.Current.CancellationToken);

        // Assert
        await using WebhooksDbContext context = new(_options);
        WebhookDeliveryAttempt attempt = await context.WebhookDeliveryAttempts.SingleAsync(TestContext.Current.CancellationToken);
        attempt.ErrorMessage!.Length.ShouldBe(2000);
    }

    [Fact]
    public async Task RecordFailureAsync_with_null_http_status_code()
    {
        // Arrange — timeout scenario
        SendWebhookCommand command = BuildCommand();

        // Act
        await _sut.RecordFailureAsync(command, null, 10000, "Timeout", null, TestContext.Current.CancellationToken);

        // Assert
        await using WebhooksDbContext context = new(_options);
        WebhookDeliveryAttempt attempt = await context.WebhookDeliveryAttempts.SingleAsync(TestContext.Current.CancellationToken);
        attempt.HttpStatusCode.ShouldBeNull();
    }

    [Fact]
    public async Task SuspendSubscriptionAsync_sets_status_and_audit_fields()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        await SeedSubscriptionAsync(subscriptionId);

        // Act
        await _sut.SuspendSubscriptionAsync(subscriptionId, "Too many failures", TestContext.Current.CancellationToken);

        // Assert
        await using WebhooksDbContext context = new(_options);
        WebhookSubscription? subscription = await context.WebhookSubscriptions.FindAsync([subscriptionId], TestContext.Current.CancellationToken);
        subscription!.Status.ShouldBe(WebhookSubscriptionStatus.Suspended);
        subscription.DeactivationReason.ShouldBe("Too many failures");
        subscription.SuspendedAt.ShouldBe(_now);
        subscription.SuspendedBy.ShouldBe("system");
    }

    [Fact]
    public async Task SuspendSubscriptionAsync_no_op_when_not_found()
    {
        // Act & Assert — should not throw
        await Should.NotThrowAsync(async () =>
            await _sut.SuspendSubscriptionAsync(Guid.NewGuid(), "reason", TestContext.Current.CancellationToken));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task SeedSubscriptionAsync(Guid subscriptionId, int consecutiveFailures = 0)
    {
        await using WebhooksDbContext context = new(_options);
        context.WebhookSubscriptions.Add(new WebhookSubscription
        {
            Id = subscriptionId,
            TargetUrl = "https://example.com/hook",
            EventType = "test.event",
            SigningSecret = "protected-secret",
            Status = WebhookSubscriptionStatus.Active,
            ConsecutiveFailureCount = consecutiveFailures,
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static SendWebhookCommand BuildCommand(Guid? subscriptionId = null) => new()
    {
        DeliveryId = Guid.NewGuid(),
        SubscriptionId = subscriptionId ?? Guid.NewGuid(),
        TargetUrl = "https://example.com/hook",
        SigningSecret = "protected-secret",
        Envelope = new WebhookEnvelope
        {
            EventId = Guid.NewGuid(),
            EventType = "test.event",
            TenantId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            ApiVersion = "1.0",
            Data = System.Text.Json.JsonSerializer.SerializeToElement(new { id = 1 }),
        },
    };
}

internal sealed class TestWebhooksDbContextFactory(DbContextOptions<WebhooksDbContext> options)
    : IDbContextFactory<WebhooksDbContext>
{
    public WebhooksDbContext CreateDbContext() => new(options);

    public Task<WebhooksDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new WebhooksDbContext(options));
}
