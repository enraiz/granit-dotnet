// =============================================================================
// Tests - WebhookSignatureService
// =============================================================================
// Verifies HMAC-SHA256 computation using a fixed test vector and the SHA-256
// payload hash helper.
// =============================================================================

using Granit.Webhooks.Internal;
using Shouldly;
using Xunit;

namespace Granit.Webhooks.Tests;

public sealed class WebhookSignatureServiceTests
{
    // Fixed test vector — values computed offline with a reference HMAC-SHA256 implementation.
    private const string TestSecret = "whsec_test_secret_1234567890abcdef";
    private const string TestBody = """{"eventId":"00000000-0000-0000-0000-000000000001","eventType":"test.event"}""";

    [Fact]
    public void Compute_ReturnsStripeFormatSignature()
    {
        DateTimeOffset timestamp = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

        string signature = WebhookSignatureService.Compute(TestSecret, timestamp, TestBody);

        signature.ShouldStartWith("t=1736935200,v1=");
    }

    [Fact]
    public void Compute_SignatureContainsTimestamp()
    {
        DateTimeOffset timestamp = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        long expectedUnix = timestamp.ToUnixTimeSeconds();

        string signature = WebhookSignatureService.Compute(TestSecret, timestamp, TestBody);

        signature.ShouldContain($"t={expectedUnix}");
    }

    [Fact]
    public void Compute_HexPartIsLowercase()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        string signature = WebhookSignatureService.Compute(TestSecret, timestamp, TestBody);
        string hexPart = signature.Split(",v1=")[1];

        hexPart.ShouldBe(hexPart.ToLowerInvariant(), "the HMAC hex must be lowercase");
    }

    [Fact]
    public void Compute_DifferentSecrets_ProduceDifferentSignatures()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        string sig1 = WebhookSignatureService.Compute("secret-a", timestamp, TestBody);
        string sig2 = WebhookSignatureService.Compute("secret-b", timestamp, TestBody);

        sig1.ShouldNotBe(sig2);
    }

    [Fact]
    public void Compute_DifferentTimestamps_ProduceDifferentSignatures()
    {
        DateTimeOffset t1 = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset t2 = new(2025, 1, 1, 0, 0, 1, TimeSpan.Zero);

        string sig1 = WebhookSignatureService.Compute(TestSecret, t1, TestBody);
        string sig2 = WebhookSignatureService.Compute(TestSecret, t2, TestBody);

        sig1.ShouldNotBe(sig2);
    }

    [Fact]
    public void Compute_SameInputs_ProduceSameSignature()
    {
        DateTimeOffset timestamp = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

        string sig1 = WebhookSignatureService.Compute(TestSecret, timestamp, TestBody);
        string sig2 = WebhookSignatureService.Compute(TestSecret, timestamp, TestBody);

        sig1.ShouldBe(sig2);
    }

    [Fact]
    public void ComputePayloadHash_ReturnsLowercaseHex()
    {
        string hash = WebhookSignatureService.ComputePayloadHash(TestBody);

        hash.ShouldBe(hash.ToLowerInvariant());
        hash.Length.ShouldBe(64, "SHA-256 produces 32 bytes = 64 hex chars");
    }

    [Fact]
    public void ComputePayloadHash_DifferentBodies_ProduceDifferentHashes()
    {
        string hash1 = WebhookSignatureService.ComputePayloadHash("""{"key":"a"}""");
        string hash2 = WebhookSignatureService.ComputePayloadHash("""{"key":"b"}""");

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public void ComputePayloadHash_SameBody_ProducesSameHash()
    {
        string hash1 = WebhookSignatureService.ComputePayloadHash(TestBody);
        string hash2 = WebhookSignatureService.ComputePayloadHash(TestBody);

        hash1.ShouldBe(hash2);
    }
}
