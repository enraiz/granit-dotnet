// =============================================================================
// Tests - FoundationCachingHybridModule
// =============================================================================
// Vérifie que le module Hybrid :
//   - Enregistre HybridCache
//   - Surcharge ICacheService<T> par HybridCacheService<T>
//   - Configure LocalCacheExpiration depuis la configuration
// =============================================================================

using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.Caching.StackExchangeRedis;
using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DigitalDynamics.Foundation.Caching.Hybrid.Tests;

public sealed class FoundationCachingHybridModuleTests
{
    private static ServiceProvider BuildServiceProvider(Action<HostApplicationBuilder>? configure = null)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Désactiver Redis (pas de serveur Redis disponible en test)
        builder.Configuration["Cache:Redis:IsEnabled"] = "false";

        configure?.Invoke(builder);

        ServiceConfigurationContext context = new(builder.Services, builder.Configuration, builder);

        // Ordre de chargement : Caching → Redis → Hybrid (géré par [DependsOn] en production)
        new FoundationCachingModule().ConfigureServices(context);
        new FoundationCachingRedisModule().ConfigureServices(context);
        new FoundationCachingHybridModule().ConfigureServices(context);

        return builder.Services.BuildServiceProvider();
    }

    [Fact]
    public void ConfigureServices_RegistersHybridCacheService_OverridesDistributedCacheService()
    {
        // Arrange & Act
        ServiceProvider sp = BuildServiceProvider();

        // Assert — ICacheService<T> doit être HybridCacheService<T>, pas DistributedCacheService<T>
        ICacheService<TestCacheItem> service = sp.GetRequiredService<ICacheService<TestCacheItem>>();
        service.Should().BeOfType<HybridCacheService<TestCacheItem>>(
            "HybridCacheService doit surcharger DistributedCacheService après AddFoundationCachingHybrid");
    }

    [Fact]
    public void ConfigureServices_HybridCacheOptionsConfigured_DefaultLocalExpiration()
    {
        // Arrange & Act
        ServiceProvider sp = BuildServiceProvider();

        // Assert
        HybridCachingOptions opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HybridCachingOptions>>().Value;
        opts.LocalCacheExpiration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void ConfigureServices_CustomLocalExpiration_IsApplied()
    {
        // Arrange
        ServiceProvider sp = BuildServiceProvider(builder =>
        {
            builder.Configuration["Cache:Hybrid:LocalCacheExpiration"] = "00:00:45";
        });

        // Act
        HybridCachingOptions opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HybridCachingOptions>>().Value;

        // Assert
        opts.LocalCacheExpiration.Should().Be(TimeSpan.FromSeconds(45));
    }

    // Type de test
    private sealed class TestCacheItem { }
}
