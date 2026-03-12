// =============================================================================
// Tests - ClaimCheckExtensions
// =============================================================================
// Verifies typed JSON serialization/deserialization helpers for IClaimCheckStore.
// =============================================================================

using Granit.Wolverine.ClaimCheck;
using Granit.Wolverine.ClaimCheck.Internal;
using Shouldly;
using Xunit;

namespace Granit.Wolverine.Tests.ClaimCheck;

public sealed class ClaimCheckExtensionsTests
{
    private readonly InMemoryClaimCheckStore _store = new();

    [Fact]
    public async Task StorePayloadAsync_SerializesAndReturnsReference()
    {
        var payload = new TestPayload("Alice", 42);

        ClaimCheckReference reference = await _store.StorePayloadAsync(
            payload, cancellationToken: TestContext.Current.CancellationToken);

        reference.ShouldNotBeNull();
        reference.ReferenceId.ShouldNotBe(Guid.Empty);
        reference.PayloadType.ShouldContain(nameof(TestPayload));
        reference.ContentType.ShouldBe("application/json");
    }

    [Fact]
    public async Task RetrievePayloadAsync_DeserializesCorrectly()
    {
        var payload = new TestPayload("Bob", 99);

        ClaimCheckReference reference = await _store.StorePayloadAsync(
            payload, cancellationToken: TestContext.Current.CancellationToken);
        TestPayload? retrieved = await _store.RetrievePayloadAsync<TestPayload>(
            reference, TestContext.Current.CancellationToken);

        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe("Bob");
        retrieved.Value.ShouldBe(99);
    }

    [Fact]
    public async Task RetrievePayloadAsync_ExpiredReference_ReturnsNull()
    {
        var reference = ClaimCheckReference.Create<TestPayload>(Guid.NewGuid());

        TestPayload? result = await _store.RetrievePayloadAsync<TestPayload>(
            reference, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ConsumePayloadAsync_RetrievesAndDeletes()
    {
        var payload = new TestPayload("Charlie", 7);

        ClaimCheckReference reference = await _store.StorePayloadAsync(
            payload, cancellationToken: TestContext.Current.CancellationToken);
        TestPayload? consumed = await _store.ConsumePayloadAsync<TestPayload>(
            reference, TestContext.Current.CancellationToken);

        consumed.ShouldNotBeNull();
        consumed.Name.ShouldBe("Charlie");

        // Second retrieval should return null (consumed)
        TestPayload? second = await _store.RetrievePayloadAsync<TestPayload>(
            reference, TestContext.Current.CancellationToken);
        second.ShouldBeNull();
    }

    [Fact]
    public async Task ConsumePayloadAsync_MissingReference_ReturnsNull()
    {
        var reference = ClaimCheckReference.Create<TestPayload>(Guid.NewGuid());

        TestPayload? result = await _store.ConsumePayloadAsync<TestPayload>(
            reference, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task StorePayloadAsync_WithExpiry_StoresSuccessfully()
    {
        var payload = new TestPayload("Dave", 1);

        ClaimCheckReference reference = await _store.StorePayloadAsync(
            payload, TimeSpan.FromMinutes(30), TestContext.Current.CancellationToken);

        TestPayload? retrieved = await _store.RetrievePayloadAsync<TestPayload>(
            reference, TestContext.Current.CancellationToken);
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe("Dave");
    }

    private sealed record TestPayload(string Name, int Value);
}
