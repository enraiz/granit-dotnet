using FluentAssertions;
using Granit.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Granit.Caching.Hybrid.Tests;

public sealed class HybridCacheServiceTests
{
    // --- Test double ---

    private sealed class TrackingHybridCache : HybridCache
    {
        private readonly Dictionary<string, object?> _store = new();
        public List<string> GetKeys { get; } = [];
        public List<string> SetKeys { get; } = [];
        public List<string> RemovedKeys { get; } = [];

        public override async ValueTask<T> GetOrCreateAsync<TState, T>(
            string key, TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            GetKeys.Add(key);
            if (_store.TryGetValue(key, out object? val) && val is T typed)
            {
                return typed;
            }

            T result = await factory(state, cancellationToken);
            _store[key] = result;
            return result;
        }

        public override ValueTask SetAsync<T>(
            string key, T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            SetKeys.Add(key);
            _store[key] = value;
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            RemovedKeys.Add(key);
            _store.Remove(key);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }

    private sealed class TestCacheItem
    {
        public string Value { get; set; } = string.Empty;
    }

    private static HybridCacheService<TestCacheItem> BuildService(
        TrackingHybridCache? cache = null,
        string keyPrefix = "dd")
    {
        cache ??= new TrackingHybridCache();
        CachingOptions options = new() { KeyPrefix = keyPrefix };
        return new HybridCacheService<TestCacheItem>(
            cache,
            Options.Create(options),
            NullLogger<HybridCacheService<TestCacheItem>>.Instance);
    }

    // -------------------------------------------------------------------------
    // GetAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_CacheMiss_ReturnsNull()
    {
        HybridCacheService<TestCacheItem> service = BuildService();

        TestCacheItem? result = await service.GetAsync("missing", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_UsesCompositeKey()
    {
        TrackingHybridCache cache = new();
        HybridCacheService<TestCacheItem> service = BuildService(cache, "app");

        await service.GetAsync("user-1", TestContext.Current.CancellationToken);

        cache.GetKeys.Should().ContainSingle()
             .Which.Should().Be("app:Test:user-1");
    }

    // -------------------------------------------------------------------------
    // GetOrAddAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrAddAsync_CacheMiss_CallsFactory()
    {
        HybridCacheService<TestCacheItem> service = BuildService();
        bool factoryCalled = false;

        TestCacheItem result = await service.GetOrAddAsync(
            "new-key",
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult(new TestCacheItem { Value = "created" });
            },
            ct: TestContext.Current.CancellationToken);

        factoryCalled.Should().BeTrue();
        result.Value.Should().Be("created");
    }

    [Fact]
    public async Task GetOrAddAsync_CacheHit_ReturnsExistingValue()
    {
        TrackingHybridCache cache = new();
        HybridCacheService<TestCacheItem> service = BuildService(cache);

        // Warm up cache
        await service.GetOrAddAsync("key",
            _ => Task.FromResult(new TestCacheItem { Value = "first" }),
            ct: TestContext.Current.CancellationToken);

        int factoryCallCount = 0;
        TestCacheItem result = await service.GetOrAddAsync("key",
            _ =>
            {
                factoryCallCount++;
                return Task.FromResult(new TestCacheItem { Value = "second" });
            },
            ct: TestContext.Current.CancellationToken);

        // Factory should NOT be called on cache hit
        factoryCallCount.Should().Be(0);
        result.Value.Should().Be("first");
    }

    [Fact]
    public async Task GetOrAddAsync_WithOptions_PassesOptions()
    {
        TrackingHybridCache cache = new();
        HybridCacheService<TestCacheItem> service = BuildService(cache);
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        };

        await service.GetOrAddAsync("key",
            _ => Task.FromResult(new TestCacheItem { Value = "v" }),
            options,
            TestContext.Current.CancellationToken);

        cache.GetKeys.Should().ContainSingle();
    }

    [Fact]
    public async Task GetOrAddAsync_WithSlidingExpiration_PassesAsLocalCacheExpiration()
    {
        TrackingHybridCache cache = new();
        HybridCacheService<TestCacheItem> service = BuildService(cache);
        DistributedCacheEntryOptions options = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
        };

        TestCacheItem result = await service.GetOrAddAsync("key",
            _ => Task.FromResult(new TestCacheItem { Value = "v" }),
            options,
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrAddAsync_WithAbsoluteExpiration_ConvertsToRelative()
    {
        TrackingHybridCache cache = new();
        HybridCacheService<TestCacheItem> service = BuildService(cache);
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1),
        };

        TestCacheItem result = await service.GetOrAddAsync("key",
            _ => Task.FromResult(new TestCacheItem { Value = "v" }),
            options,
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // SetAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetAsync_StoresValue()
    {
        TrackingHybridCache cache = new();
        HybridCacheService<TestCacheItem> service = BuildService(cache);

        await service.SetAsync("key", new TestCacheItem { Value = "stored" },
            ct: TestContext.Current.CancellationToken);

        cache.SetKeys.Should().ContainSingle()
             .Which.Should().Contain("key");
    }

    [Fact]
    public async Task SetAsync_WithOptions_PassesOptions()
    {
        TrackingHybridCache cache = new();
        HybridCacheService<TestCacheItem> service = BuildService(cache);
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
        };

        await service.SetAsync("key", new TestCacheItem { Value = "v" }, options,
            TestContext.Current.CancellationToken);

        cache.SetKeys.Should().ContainSingle();
    }

    // -------------------------------------------------------------------------
    // RemoveAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveAsync_RemovesEntry()
    {
        TrackingHybridCache cache = new();
        HybridCacheService<TestCacheItem> service = BuildService(cache);

        await service.RemoveAsync("key", TestContext.Current.CancellationToken);

        cache.RemovedKeys.Should().ContainSingle()
             .Which.Should().Contain("key");
    }

    // -------------------------------------------------------------------------
    // RefreshAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshAsync_DoesNotThrow()
    {
        HybridCacheService<TestCacheItem> service = BuildService();

        Func<Task> act = () => service.RefreshAsync("key", TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------------
    // Key format
    // -------------------------------------------------------------------------

    [Fact]
    public async Task BuildKey_Format_Is_Prefix_CacheName_UserKey()
    {
        TrackingHybridCache cache = new();
        HybridCacheService<TestCacheItem> service = BuildService(cache, "myapp");

        await service.RemoveAsync("patient-42", TestContext.Current.CancellationToken);

        cache.RemovedKeys.Should().ContainSingle()
             .Which.Should().Be("myapp:Test:patient-42");
    }
}
