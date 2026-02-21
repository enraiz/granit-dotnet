// =============================================================================
// Tests - TypedKeyCacheServiceAdapter<TCacheItem, TKey>
// =============================================================================
// Vérifie que chaque méthode de l'adaptateur délègue correctement à
// l'ICacheService<TCacheItem> sous-jacent en convertissant la clé typée
// via key.ToString().
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.Caching.Tests;

public sealed class TypedKeyCacheServiceAdapterTests
{
    public sealed class Item
    {
        public string Value { get; init; } = string.Empty;
    }

    private static (TypedKeyCacheServiceAdapter<Item, Guid> adapter, ICacheService<Item> inner) CreateAdapter()
    {
        ICacheService<Item> inner = Substitute.For<ICacheService<Item>>();
        TypedKeyCacheServiceAdapter<Item, Guid> adapter = new(inner);
        return (adapter, inner);
    }

    [Fact]
    public async Task GetAsync_StringKey_DelegatesToInner()
    {
        (TypedKeyCacheServiceAdapter<Item, Guid> adapter, ICacheService<Item> inner) = CreateAdapter();
        Item expected = new() { Value = "test" };
        inner.GetAsync("key", Arg.Any<CancellationToken>()).Returns(expected);

        Item? result = await adapter.GetAsync("key", TestContext.Current.CancellationToken);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetAsync_TypedKey_ConvertsToStringAndDelegates()
    {
        (TypedKeyCacheServiceAdapter<Item, Guid> adapter, ICacheService<Item> inner) = CreateAdapter();
        Guid id = Guid.NewGuid();
        Item expected = new() { Value = "typed" };
        inner.GetAsync(id.ToString(), Arg.Any<CancellationToken>()).Returns(expected);

        Item? result = await adapter.GetAsync(id, TestContext.Current.CancellationToken);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetOrAddAsync_StringKey_DelegatesToInner()
    {
        (TypedKeyCacheServiceAdapter<Item, Guid> adapter, ICacheService<Item> inner) = CreateAdapter();
        Item expected = new() { Value = "added" };
        inner.GetOrAddAsync(
                "key",
                Arg.Any<Func<CancellationToken, Task<Item>>>(),
                Arg.Any<DistributedCacheEntryOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        Item result = await adapter.GetOrAddAsync(
            "key",
            _ => Task.FromResult(expected),
            null,
            TestContext.Current.CancellationToken);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetOrAddAsync_TypedKey_ConvertsToStringAndDelegates()
    {
        (TypedKeyCacheServiceAdapter<Item, Guid> adapter, ICacheService<Item> inner) = CreateAdapter();
        Guid id = Guid.NewGuid();
        Item expected = new() { Value = "typed-added" };
        inner.GetOrAddAsync(
                id.ToString(),
                Arg.Any<Func<CancellationToken, Task<Item>>>(),
                Arg.Any<DistributedCacheEntryOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        Item result = await adapter.GetOrAddAsync(
            id,
            _ => Task.FromResult(expected),
            null,
            TestContext.Current.CancellationToken);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task SetAsync_StringKey_DelegatesToInner()
    {
        (TypedKeyCacheServiceAdapter<Item, Guid> adapter, ICacheService<Item> inner) = CreateAdapter();
        Item item = new() { Value = "set" };

        await adapter.SetAsync("key", item, null, TestContext.Current.CancellationToken);

        await inner.Received(1).SetAsync("key", item, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_TypedKey_ConvertsToStringAndDelegates()
    {
        (TypedKeyCacheServiceAdapter<Item, Guid> adapter, ICacheService<Item> inner) = CreateAdapter();
        Guid id = Guid.NewGuid();
        Item item = new() { Value = "typed-set" };

        await adapter.SetAsync(id, item, null, TestContext.Current.CancellationToken);

        await inner.Received(1).SetAsync(id.ToString(), item, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_StringKey_DelegatesToInner()
    {
        (TypedKeyCacheServiceAdapter<Item, Guid> adapter, ICacheService<Item> inner) = CreateAdapter();

        await adapter.RemoveAsync("key", TestContext.Current.CancellationToken);

        await inner.Received(1).RemoveAsync("key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_TypedKey_ConvertsToStringAndDelegates()
    {
        (TypedKeyCacheServiceAdapter<Item, Guid> adapter, ICacheService<Item> inner) = CreateAdapter();
        Guid id = Guid.NewGuid();

        await adapter.RemoveAsync(id, TestContext.Current.CancellationToken);

        await inner.Received(1).RemoveAsync(id.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_StringKey_DelegatesToInner()
    {
        (TypedKeyCacheServiceAdapter<Item, Guid> adapter, ICacheService<Item> inner) = CreateAdapter();

        await adapter.RefreshAsync("key", TestContext.Current.CancellationToken);

        await inner.Received(1).RefreshAsync("key", Arg.Any<CancellationToken>());
    }
}
