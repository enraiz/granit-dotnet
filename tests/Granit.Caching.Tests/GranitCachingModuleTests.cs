// =============================================================================
// Tests - GranitCachingModule
// =============================================================================
// Vérifie que le module enregistre correctement :
//   - ICacheService<T> → DistributedCacheService<T>
//   - ICacheService<T, TKey> → TypedKeyCacheServiceAdapter<T, TKey>
//   - ICacheValueEncryptor → NullCacheValueEncryptor (par défaut)
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Granit.Caching.Tests;

public sealed class GranitCachingModuleTests
{
    private static ServiceProvider BuildServiceProvider()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        GranitCachingModule module = new();
        Granit.Core.Modularity.ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        module.ConfigureServices(context);
        return builder.Services.BuildServiceProvider();
    }

    [Fact]
    public void ConfigureServices_RegistersStringKeyCacheService()
    {
        // Arrange & Act
        ServiceProvider sp = BuildServiceProvider();

        // Assert
        ICacheService<TestCacheItem> service = sp.GetRequiredService<ICacheService<TestCacheItem>>();
        service.ShouldBeOfType<DistributedCacheService<TestCacheItem>>();
    }

    [Fact]
    public void ConfigureServices_RegistersTypedKeyCacheService()
    {
        // Arrange & Act
        ServiceProvider sp = BuildServiceProvider();

        // Assert
        ICacheService<TestCacheItem, Guid> service = sp.GetRequiredService<ICacheService<TestCacheItem, Guid>>();
        service.ShouldNotBeNull();
    }

    [Fact]
    public void ConfigureServices_RegistersNullEncryptor_ByDefault()
    {
        // Arrange & Act
        ServiceProvider sp = BuildServiceProvider();

        // Assert — en l'absence de configuration EncryptValues, l'encrypteur no-op est utilisé
        ICacheValueEncryptor encryptor = sp.GetRequiredService<ICacheValueEncryptor>();
        encryptor.ShouldBeOfType<NullCacheValueEncryptor>();
    }

    // Type de test
    private sealed class TestCacheItem { }
}
