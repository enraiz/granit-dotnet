// =============================================================================
// Tests - NullWebhookDeliveryReader
// =============================================================================
// Verifies that the no-op reader always returns null.
// =============================================================================

using Granit.Webhooks.Domain;
using Granit.Webhooks.Internal;
using Shouldly;
using Xunit;

namespace Granit.Webhooks.Tests;

public sealed class NullWebhookDeliveryReaderTests
{
    private readonly NullWebhookDeliveryReader _reader = new();

    [Fact]
    public async Task FindByDeliveryIdAsync_ReturnsNull()
    {
        WebhookDeliveryAttempt? result = await _reader.FindByDeliveryIdAsync(
            Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task FindByDeliveryIdAsync_EmptyGuid_ReturnsNull()
    {
        WebhookDeliveryAttempt? result = await _reader.FindByDeliveryIdAsync(
            Guid.Empty, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task FindByDeliveryIdAsync_MultipleCalls_AlwaysReturnsNull()
    {
        WebhookDeliveryAttempt? first = await _reader.FindByDeliveryIdAsync(
            Guid.NewGuid(), TestContext.Current.CancellationToken);
        WebhookDeliveryAttempt? second = await _reader.FindByDeliveryIdAsync(
            Guid.NewGuid(), TestContext.Current.CancellationToken);

        first.ShouldBeNull();
        second.ShouldBeNull();
    }
}
