// =============================================================================
// Tests - DistributedCacheService<TCacheItem> (clé string)
// =============================================================================
// Vérifie les opérations CRUD, la convention de clé composite,
// et le chiffrement AES opt-in via CacheEncryptedAttribute.
// =============================================================================

using Granit.Caching.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Caching.Tests;

public sealed class DistributedCacheServiceTests
{
    private static DistributedCacheService<T> CreateService<T>(
        IDistributedCache? cache = null,
        ICacheValueEncryptor? encryptor = null,
        CachingOptions? options = null)
        where T : class
    {
        IDistributedCache distributedCache = cache ?? Substitute.For<IDistributedCache>();
        IMemoryCache lockCache = new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions { SizeLimit = 100 }));
        ICacheValueEncryptor cacheEncryptor = encryptor ?? new NullCacheValueEncryptor();
        CachingOptions cachingOptions = options ?? new CachingOptions();

        return new DistributedCacheService<T>(
            distributedCache,
            lockCache,
            cacheEncryptor,
            Microsoft.Extensions.Options.Options.Create(cachingOptions),
            NullLogger<DistributedCacheService<T>>.Instance);
    }

    private static ServiceProvider BuildInMemoryServiceProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddKeyedSingleton<IMemoryCache>(
            DistributedCacheService<UserCacheItem>.LockCacheKey,
            (_, _) => new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions { SizeLimit = 100 })));
        services.AddSingleton<ICacheValueEncryptor, NullCacheValueEncryptor>();
        services.Configure<CachingOptions>(_ => { });
        services.AddSingleton(typeof(ICacheService<>), typeof(DistributedCacheService<>));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetAsync_KeyNotInCache_ReturnsNull()
    {
        // Arrange
        IDistributedCache distributedCache = Substitute.For<IDistributedCache>();
        distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        DistributedCacheService<UserCacheItem> sut = CreateService<UserCacheItem>(distributedCache);

        // Act
        UserCacheItem? result = await sut.GetAsync("user-1", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsOriginalValue()
    {
        // Arrange
        ServiceProvider sp = BuildInMemoryServiceProvider();
        ICacheService<UserCacheItem> sut = sp.GetRequiredService<ICacheService<UserCacheItem>>();

        UserCacheItem expected = new() { Id = Guid.NewGuid(), Name = "Alice" };

        // Act — paramètre nommé "cancellationToken" (convention interface ICacheService)
        await sut.SetAsync("alice", expected, null, TestContext.Current.CancellationToken);
        UserCacheItem? result = await sut.GetAsync("alice", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(expected.Id);
        result.Name.ShouldBe(expected.Name);
    }

    [Fact]
    public async Task RemoveAsync_KeyExists_SubsequentGetReturnsNull()
    {
        // Arrange
        ServiceProvider sp = BuildInMemoryServiceProvider();
        ICacheService<UserCacheItem> sut = sp.GetRequiredService<ICacheService<UserCacheItem>>();

        await sut.SetAsync(
            "user-to-remove",
            new UserCacheItem { Id = Guid.NewGuid(), Name = "Bob" },
            null,
            TestContext.Current.CancellationToken);

        // Act
        await sut.RemoveAsync("user-to-remove", TestContext.Current.CancellationToken);
        UserCacheItem? result = await sut.GetAsync("user-to-remove", TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetOrAddAsync_KeyNotInCache_CallsFactoryOnce()
    {
        // Arrange
        ServiceProvider sp = BuildInMemoryServiceProvider();
        ICacheService<UserCacheItem> sut = sp.GetRequiredService<ICacheService<UserCacheItem>>();

        int callCount = 0;
        UserCacheItem expected = new() { Id = Guid.NewGuid(), Name = "Charlie" };

        // Act
        UserCacheItem result = await sut.GetOrAddAsync(
            "charlie",
            async cancellationToken =>
            {
                callCount++;
                await Task.Delay(1, cancellationToken);
                return expected;
            },
            null,
            TestContext.Current.CancellationToken);

        // Assert
        result.Id.ShouldBe(expected.Id);
        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrAddAsync_KeyAlreadyInCache_DoesNotCallFactory()
    {
        // Arrange
        ServiceProvider sp = BuildInMemoryServiceProvider();
        ICacheService<UserCacheItem> sut = sp.GetRequiredService<ICacheService<UserCacheItem>>();

        UserCacheItem existing = new() { Id = Guid.NewGuid(), Name = "Diana" };
        await sut.SetAsync("diana", existing, null, TestContext.Current.CancellationToken);

        int callCount = 0;

        // Act
        UserCacheItem result = await sut.GetOrAddAsync(
            "diana",
            _ =>
            {
                callCount++;
                return Task.FromResult(new UserCacheItem { Id = Guid.NewGuid(), Name = "SHOULD_NOT_APPEAR" });
            },
            null,
            TestContext.Current.CancellationToken);

        // Assert
        result.Id.ShouldBe(existing.Id);
        callCount.ShouldBe(0);
    }

    [Fact]
    public async Task BuildKey_UsesKeyPrefixAndCacheName()
    {
        // Arrange — clé composite attendue : "myapp:User:user-42"
        IDistributedCache distributedCache = Substitute.For<IDistributedCache>();
        string? capturedKey = null;

        // Retourne null explicitement et capture la clé passée à GetAsync
        distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(callInfo =>
            {
                capturedKey = callInfo.Arg<string>();
                return Task.FromResult<byte[]?>(null);
            });

        CachingOptions options = new() { KeyPrefix = "myapp" };
        DistributedCacheService<UserCacheItem> sut = CreateService<UserCacheItem>(distributedCache, options: options);

        // Act
        _ = await sut.GetAsync("user-42", TestContext.Current.CancellationToken);

        // Assert
        capturedKey.ShouldBe("myapp:User:user-42");
    }

    // Types de test
    public sealed class UserCacheItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
