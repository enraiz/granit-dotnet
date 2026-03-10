// =============================================================================
// Tests - NoOpWebhookSecretProtector
// =============================================================================
// Verifies that the pass-through protector returns secrets unchanged.
// =============================================================================

using Granit.Webhooks.Internal;
using Shouldly;
using Xunit;

namespace Granit.Webhooks.Tests;

public sealed class NoOpWebhookSecretProtectorTests
{
    private readonly NoOpWebhookSecretProtector _protector = new();

    [Fact]
    public async Task ProtectAsync_ReturnsPlainSecretUnchanged()
    {
        var secret = "test-webhook-value-256";

        var result = await _protector.ProtectAsync(secret, TestContext.Current.CancellationToken);

        result.ShouldBe(secret);
    }

    [Fact]
    public async Task UnprotectAsync_ReturnsProtectedSecretUnchanged()
    {
        var secret = "my-protected-secret";

        var result = await _protector.UnprotectAsync(secret, TestContext.Current.CancellationToken);

        result.ShouldBe(secret);
    }

    [Fact]
    public async Task ProtectAsync_EmptyString_ReturnsEmptyString()
    {
        var result = await _protector.ProtectAsync(string.Empty, TestContext.Current.CancellationToken);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task UnprotectAsync_EmptyString_ReturnsEmptyString()
    {
        var result = await _protector.UnprotectAsync(string.Empty, TestContext.Current.CancellationToken);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task RoundTrip_ProtectThenUnprotect_ReturnsSameValue()
    {
        var original = "round-trip-secret";

        var protectedValue = await _protector.ProtectAsync(original, TestContext.Current.CancellationToken);
        var unprotected = await _protector.UnprotectAsync(protectedValue, TestContext.Current.CancellationToken);

        unprotected.ShouldBe(original);
    }
}
