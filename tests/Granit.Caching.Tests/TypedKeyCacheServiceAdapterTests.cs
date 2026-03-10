// =============================================================================
// Tests - TypedKeyCacheServiceAdapter<TCacheItem, TKey>
// =============================================================================
// Vérifie que l'adaptateur convertit correctement la clé typée en string via
// key.ToString() et délègue à ICacheService<TCacheItem> sous-jacent.
// Utilise un ServiceProvider in-memory (même pattern que DistributedCacheServiceTests)
// pour éviter d'exposer des types internes aux générateurs de proxy NSubstitute.
// =============================================================================

using Granit.Caching.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Caching.Tests;

public sealed class TypedKeyCacheServiceAdapterTests
{
    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddKeyedSingleton<IMemoryCache>(
            DistributedCacheService<DistributedCacheServiceTests.UserCacheItem>.LockCacheKey,
            (_, _) => new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions { SizeLimit = 100 })));
        services.AddSingleton<ICacheValueEncryptor, NullCacheValueEncryptor>();
        services.Configure<CachingOptions>(_ => { });
        services.AddSingleton(typeof(ICacheService<>), typeof(DistributedCacheService<>));
        services.AddSingleton(typeof(ICacheService<,>), typeof(TypedKeyCacheServiceAdapter<,>));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SetAsync_TypedKey_CanBeRetrievedWithStringKey()
    {
        // Arrange — écriture via clé Guid, lecture via clé string équivalente
        ServiceProvider sp = BuildServiceProvider();
        ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid> typedSvc =
            sp.GetRequiredService<ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid>>();
        ICacheService<DistributedCacheServiceTests.UserCacheItem> stringSvc =
            sp.GetRequiredService<ICacheService<DistributedCacheServiceTests.UserCacheItem>>();

        var id = Guid.NewGuid();
        DistributedCacheServiceTests.UserCacheItem item = new() { Id = id, Name = "Alice" };

        // Act
        await typedSvc.SetAsync(id, item, null, TestContext.Current.CancellationToken);
        DistributedCacheServiceTests.UserCacheItem? result =
            await stringSvc.GetAsync(id.ToString(), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task GetAsync_TypedKey_ReturnsValueStoredByStringKey()
    {
        // Arrange — écriture via clé string, lecture via clé Guid
        ServiceProvider sp = BuildServiceProvider();
        ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid> typedSvc =
            sp.GetRequiredService<ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid>>();
        ICacheService<DistributedCacheServiceTests.UserCacheItem> stringSvc =
            sp.GetRequiredService<ICacheService<DistributedCacheServiceTests.UserCacheItem>>();

        var id = Guid.NewGuid();
        DistributedCacheServiceTests.UserCacheItem item = new() { Id = id, Name = "Bob" };

        // Act
        await stringSvc.SetAsync(id.ToString(), item, null, TestContext.Current.CancellationToken);
        DistributedCacheServiceTests.UserCacheItem? result =
            await typedSvc.GetAsync(id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Bob");
    }

    [Fact]
    public async Task GetOrAddAsync_TypedKey_CallsFactoryOnMissAndCachesResult()
    {
        // Arrange
        ServiceProvider sp = BuildServiceProvider();
        ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid> typedSvc =
            sp.GetRequiredService<ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid>>();

        var id = Guid.NewGuid();
        int callCount = 0;

        // Act
        DistributedCacheServiceTests.UserCacheItem result = await typedSvc.GetOrAddAsync(
            id,
            async cancellationToken =>
            {
                callCount++;
                await Task.Delay(1, cancellationToken);
                return new DistributedCacheServiceTests.UserCacheItem { Id = id, Name = "Charlie" };
            },
            null,
            TestContext.Current.CancellationToken);

        // Assert
        result.Id.ShouldBe(id);
        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveAsync_TypedKey_InvalidatesEntry()
    {
        // Arrange
        ServiceProvider sp = BuildServiceProvider();
        ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid> typedSvc =
            sp.GetRequiredService<ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid>>();

        var id = Guid.NewGuid();
        await typedSvc.SetAsync(
            id,
            new DistributedCacheServiceTests.UserCacheItem { Id = id, Name = "Diana" },
            null,
            TestContext.Current.CancellationToken);

        // Act
        await typedSvc.RemoveAsync(id, TestContext.Current.CancellationToken);
        DistributedCacheServiceTests.UserCacheItem? result =
            await typedSvc.GetAsync(id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_StringKey_DelegatesToInner()
    {
        // Arrange — vérifie la surcharge clé string héritée de ICacheService<T>
        ServiceProvider sp = BuildServiceProvider();
        ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid> typedSvc =
            sp.GetRequiredService<ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid>>();

        DistributedCacheServiceTests.UserCacheItem item = new() { Id = Guid.NewGuid(), Name = "Eve" };
        await typedSvc.SetAsync("string-key", item, null, TestContext.Current.CancellationToken);

        // Act
        DistributedCacheServiceTests.UserCacheItem? result =
            await typedSvc.GetAsync("string-key", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Eve");
    }

    [Fact]
    public async Task GetOrAddAsync_StringKey_DelegatesToInner()
    {
        // Arrange — vérifie la surcharge clé string héritée de ICacheService<T>
        ServiceProvider sp = BuildServiceProvider();
        ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid> typedSvc =
            sp.GetRequiredService<ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid>>();

        DistributedCacheServiceTests.UserCacheItem expected = new() { Id = Guid.NewGuid(), Name = "Frank" };

        // Act
        DistributedCacheServiceTests.UserCacheItem result = await typedSvc.GetOrAddAsync(
            "string-key-2",
            _ => Task.FromResult(expected),
            null,
            TestContext.Current.CancellationToken);

        // Assert
        result.Id.ShouldBe(expected.Id);
    }

    [Fact]
    public async Task SetAsync_StringKey_DelegatesToInner()
    {
        // Arrange — vérifie la surcharge clé string héritée de ICacheService<T>
        ServiceProvider sp = BuildServiceProvider();
        ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid> typedSvc =
            sp.GetRequiredService<ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid>>();

        DistributedCacheServiceTests.UserCacheItem item = new() { Id = Guid.NewGuid(), Name = "Grace" };

        // Act
        await typedSvc.SetAsync("string-key-3", item, null, TestContext.Current.CancellationToken);
        DistributedCacheServiceTests.UserCacheItem? result =
            await typedSvc.GetAsync("string-key-3", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RemoveAsync_StringKey_DelegatesToInner()
    {
        // Arrange — vérifie la surcharge clé string héritée de ICacheService<T>
        ServiceProvider sp = BuildServiceProvider();
        ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid> typedSvc =
            sp.GetRequiredService<ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid>>();

        DistributedCacheServiceTests.UserCacheItem item = new() { Id = Guid.NewGuid(), Name = "Heidi" };
        await typedSvc.SetAsync("remove-key", item, null, TestContext.Current.CancellationToken);

        // Act
        await typedSvc.RemoveAsync("remove-key", TestContext.Current.CancellationToken);
        DistributedCacheServiceTests.UserCacheItem? result =
            await typedSvc.GetAsync("remove-key", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task RefreshAsync_StringKey_DelegatesToInner()
    {
        // Arrange — vérifie que RefreshAsync ne lève pas d'exception sur l'adaptateur
        ServiceProvider sp = BuildServiceProvider();
        ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid> typedSvc =
            sp.GetRequiredService<ICacheService<DistributedCacheServiceTests.UserCacheItem, Guid>>();

        // Act — RefreshAsync ne retourne Task.CompletedTask si la clé n'existe pas en Memory
        Func<Task> act = () => typedSvc.RefreshAsync("refresh-key", TestContext.Current.CancellationToken);

        // Assert
        await Should.NotThrowAsync(act);
    }
}
