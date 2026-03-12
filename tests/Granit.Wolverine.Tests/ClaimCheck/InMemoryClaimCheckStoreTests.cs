// =============================================================================
// Tests - InMemoryClaimCheckStore
// =============================================================================
// Verifies store/retrieve/delete semantics of the in-memory Claim Check store.
// =============================================================================

using Granit.Wolverine.ClaimCheck.Internal;
using Shouldly;
using Xunit;

namespace Granit.Wolverine.Tests.ClaimCheck;

public sealed class InMemoryClaimCheckStoreTests
{
    private readonly InMemoryClaimCheckStore _store = new();

    [Fact]
    public async Task StoreAsync_ReturnsUniqueId()
    {
        byte[] data = [1, 2, 3];

        Guid id1 = await _store.StoreAsync(data, cancellationToken: TestContext.Current.CancellationToken);
        Guid id2 = await _store.StoreAsync(data, cancellationToken: TestContext.Current.CancellationToken);

        id1.ShouldNotBe(Guid.Empty);
        id2.ShouldNotBe(Guid.Empty);
        id1.ShouldNotBe(id2);
    }

    [Fact]
    public async Task RetrieveAsync_AfterStore_ReturnsOriginalData()
    {
        byte[] data = [10, 20, 30, 40, 50];

        Guid id = await _store.StoreAsync(data, cancellationToken: TestContext.Current.CancellationToken);
        byte[]? result = await _store.RetrieveAsync(id, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBe(data);
    }

    [Fact]
    public async Task RetrieveAsync_UnknownId_ReturnsNull()
    {
        byte[]? result = await _store.RetrieveAsync(
            Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrueAndRemovesData()
    {
        byte[] data = [1, 2, 3];
        Guid id = await _store.StoreAsync(data, cancellationToken: TestContext.Current.CancellationToken);

        bool deleted = await _store.DeleteAsync(id, TestContext.Current.CancellationToken);

        deleted.ShouldBeTrue();
        byte[]? result = await _store.RetrieveAsync(id, TestContext.Current.CancellationToken);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        bool deleted = await _store.DeleteAsync(
            Guid.NewGuid(), TestContext.Current.CancellationToken);

        deleted.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_CalledTwice_SecondCallReturnsFalse()
    {
        byte[] data = [1, 2, 3];
        Guid id = await _store.StoreAsync(data, cancellationToken: TestContext.Current.CancellationToken);

        bool first = await _store.DeleteAsync(id, TestContext.Current.CancellationToken);
        bool second = await _store.DeleteAsync(id, TestContext.Current.CancellationToken);

        first.ShouldBeTrue();
        second.ShouldBeFalse();
    }

    [Fact]
    public async Task StoreAsync_IgnoresContentTypeAndExpiry()
    {
        byte[] data = [42];

        Guid id = await _store.StoreAsync(
            data, "application/xml", TimeSpan.FromMinutes(5),
            TestContext.Current.CancellationToken);

        byte[]? result = await _store.RetrieveAsync(id, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.ShouldBe(data);
    }
}
