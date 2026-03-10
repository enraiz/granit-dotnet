// =============================================================================
// Tests - GranitCachingHybridModule
// =============================================================================
// Vérifie que le module Hybrid :
//   - Enregistre HybridCache
//   - Surcharge ICacheService<T> par HybridCacheService<T>
//   - Configure LocalCacheExpiration depuis la configuration
// =============================================================================

using Granit.Caching;
using Granit.Caching.Hybrid.Options;
using Granit.Caching.StackExchangeRedis;
using Granit.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Granit.Caching.Hybrid.Tests;

public sealed class GranitCachingHybridModuleTests
{
    private static ServiceProvider BuildServiceProvider(Action<HostApplicationBuilder>? configure = null)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Désactiver Redis (pas de serveur Redis disponible en test)
        builder.Configuration["Cache:Redis:IsEnabled"] = "false";

        configure?.Invoke(builder);

        ServiceConfigurationContext context = new(builder.Services, builder.Configuration, builder);

        // Ordre de chargement : Caching → Redis → Hybrid (géré par [DependsOn] en production)
        new GranitCachingModule().ConfigureServices(context);
        new GranitCachingRedisModule().ConfigureServices(context);
        new GranitCachingHybridModule().ConfigureServices(context);

        return builder.Services.BuildServiceProvider();
    }

    [Fact]
    public void ConfigureServices_RegistersHybridCacheService_OverridesDistributedCacheService()
    {
        // Arrange & Act
        ServiceProvider sp = BuildServiceProvider();

        // Assert — ICacheService<T> doit être HybridCacheService<T>, pas DistributedCacheService<T>
        ICacheService<TestCacheItem> service = sp.GetRequiredService<ICacheService<TestCacheItem>>();
        service.ShouldBeOfType<HybridCacheService<TestCacheItem>>("HybridCacheService doit surcharger DistributedCacheService après AddGranitCachingHybrid");
    }

    [Fact]
    public void ConfigureServices_HybridCacheOptionsConfigured_DefaultLocalExpiration()
    {
        // Arrange & Act
        ServiceProvider sp = BuildServiceProvider();

        // Assert
        HybridCachingOptions opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HybridCachingOptions>>().Value;
        opts.LocalCacheExpiration.ShouldBe(TimeSpan.FromSeconds(30));
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
        opts.LocalCacheExpiration.ShouldBe(TimeSpan.FromSeconds(45));
    }

    // Type de test
    private sealed class TestCacheItem { }
}
