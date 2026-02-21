// =============================================================================
// Tests - FoundationCachingModule
// =============================================================================
// Vérifie que le module enregistre correctement :
//   - ICacheService<T> → DistributedCacheService<T>
//   - ICacheService<T, TKey> → TypedKeyCacheServiceAdapter<T, TKey>
//   - ICacheValueEncryptor → NullCacheValueEncryptor (par défaut)
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DigitalDynamics.Foundation.Caching.Tests;

public sealed class FoundationCachingModuleTests
{
    private static ServiceProvider BuildServiceProvider()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        FoundationCachingModule module = new();
        DigitalDynamics.Foundation.Core.Modularity.ServiceConfigurationContext context = new(
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
        service.Should().BeOfType<DistributedCacheService<TestCacheItem>>();
    }

    [Fact]
    public void ConfigureServices_RegistersTypedKeyCacheService()
    {
        // Arrange & Act
        ServiceProvider sp = BuildServiceProvider();

        // Assert
        ICacheService<TestCacheItem, Guid> service = sp.GetRequiredService<ICacheService<TestCacheItem, Guid>>();
        service.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_RegistersNullEncryptor_ByDefault()
    {
        // Arrange & Act
        ServiceProvider sp = BuildServiceProvider();

        // Assert — en l'absence de configuration EncryptValues, l'encrypteur no-op est utilisé
        ICacheValueEncryptor encryptor = sp.GetRequiredService<ICacheValueEncryptor>();
        encryptor.Should().BeOfType<NullCacheValueEncryptor>();
    }

    // Type de test
    private sealed class TestCacheItem { }
}
