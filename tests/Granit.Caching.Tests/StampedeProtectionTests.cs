// =============================================================================
// Tests - Protection Stampede (cache stampede / thundering herd)
// =============================================================================
// Vérifie que GetOrAddAsync n'exécute la factory qu'une seule fois lorsque
// plusieurs threads requêtent simultanément la même clé absente du cache.
// Pattern : double-check locking + SemaphoreSlim stocké dans IMemoryCache.
// =============================================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Caching.Tests;

public sealed class StampedeProtectionTests
{
    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddKeyedSingleton<IMemoryCache>(
            DistributedCacheService<ProductCacheItem>.LockCacheKey,
            (_, _) => new MemoryCache(Options.Create(new MemoryCacheOptions { SizeLimit = 100 })));
        services.AddSingleton<ICacheValueEncryptor, NullCacheValueEncryptor>();
        services.Configure<CachingOptions>(_ => { });
        services.AddSingleton(typeof(ICacheService<>), typeof(DistributedCacheService<>));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetOrAddAsync_ConcurrentRequests_CallsFactoryExactlyOnce()
    {
        // Arrange
        ServiceProvider sp = BuildServiceProvider();
        ICacheService<ProductCacheItem> sut = sp.GetRequiredService<ICacheService<ProductCacheItem>>();

        int factoryCallCount = 0;
        ProductCacheItem product = new() { Id = 42, Name = "Widget" };
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Act — 10 requêtes concurrentes sur la même clé absente du cache
        Task<ProductCacheItem>[] tasks = [.. Enumerable.Range(0, 10)
            .Select(_ => sut.GetOrAddAsync(
                "product-42",
                async innerCt =>
                {
                    Interlocked.Increment(ref factoryCallCount);
                    await Task.Delay(50, innerCt);
                    return product;
                },
                null,
                ct))];

        ProductCacheItem[] results = await Task.WhenAll(tasks);

        // Assert — factory appelée exactement 1 fois, tous les résultats identiques
        factoryCallCount.ShouldBe(1, "la factory ne doit être exécutée qu'une seule fois sous concurrence");
        results.ToList().ForEach(r => r.Id.ShouldBe(42));
    }

    [Fact]
    public async Task GetOrAddAsync_DifferentKeys_CallsFactoryForEachKey()
    {
        // Arrange
        ServiceProvider sp = BuildServiceProvider();
        ICacheService<ProductCacheItem> sut = sp.GetRequiredService<ICacheService<ProductCacheItem>>();

        int factoryCallCount = 0;
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Act — 3 clés différentes, chacune doit appeler la factory une fois
        await Task.WhenAll(
            sut.GetOrAddAsync("key-1", _ => { Interlocked.Increment(ref factoryCallCount); return Task.FromResult(new ProductCacheItem { Id = 1 }); }, null, ct),
            sut.GetOrAddAsync("key-2", _ => { Interlocked.Increment(ref factoryCallCount); return Task.FromResult(new ProductCacheItem { Id = 2 }); }, null, ct),
            sut.GetOrAddAsync("key-3", _ => { Interlocked.Increment(ref factoryCallCount); return Task.FromResult(new ProductCacheItem { Id = 3 }); }, null, ct));

        // Assert
        factoryCallCount.ShouldBe(3);
    }

    // Type de test
    public sealed class ProductCacheItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
