// =============================================================================
// Tests - RedisCachingServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationCachingRedis :
//   - Remplace IDistributedCache par RedisCache quand IsEnabled = true
//   - Ne modifie pas les services quand IsEnabled = false
//   - Enregistre AesCacheValueEncryptor si EncryptValues = true
//   - Conserve NullCacheValueEncryptor si EncryptValues = false
// =============================================================================

using DigitalDynamics.Foundation.Caching.StackExchangeRedis.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.Caching.StackExchangeRedis.Tests;

public sealed class RedisCachingServiceCollectionExtensionsTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void AddFoundationCachingRedis_IsEnabledFalse_DoesNotReplaceDistributedCache()
    {
        // Arrange
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Cache:Redis:IsEnabled"] = "false",
        });

        ServiceCollection services = new();
        services.AddDistributedMemoryCache(); // Memory par défaut

        // Act
        services.AddFoundationCachingRedis(configuration);

        // Assert — le MemoryDistributedCache doit rester
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IDistributedCache));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().NotBe<RedisCache>();
    }

    [Fact]
    public void AddFoundationCachingRedis_IsEnabledTrue_RegistersRedisCache()
    {
        // Arrange
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Cache:Redis:IsEnabled"] = "true",
            ["Cache:Redis:Configuration"] = "localhost:6379",
            ["Cache:Redis:InstanceName"] = "test:",
        });

        ServiceCollection services = new();

        // Act
        services.AddFoundationCachingRedis(configuration);

        // Assert — RedisCache doit être enregistré pour IDistributedCache
        ServiceDescriptor? redisDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IDistributedCache));
        redisDescriptor.Should().NotBeNull("Redis doit être enregistré comme IDistributedCache");
    }

    [Fact]
    public void AddFoundationCachingRedis_EncryptValues_True_RegistersAesEncryptor()
    {
        // Arrange
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Cache:Redis:IsEnabled"] = "true",
            ["Cache:Redis:Configuration"] = "localhost:6379",
            ["Cache:EncryptValues"] = "true",
        });

        ServiceCollection services = new();

        // Act
        services.AddFoundationCachingRedis(configuration);

        // Assert
        ServiceDescriptor? encryptorDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ICacheValueEncryptor));
        encryptorDescriptor.Should().NotBeNull();
        encryptorDescriptor!.ImplementationType.Should().Be<AesCacheValueEncryptor>();
    }

    [Fact]
    public void AddFoundationCachingRedis_EncryptValues_False_DoesNotRegisterAesEncryptor()
    {
        // Arrange
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Cache:Redis:IsEnabled"] = "true",
            ["Cache:Redis:Configuration"] = "localhost:6379",
            ["Cache:EncryptValues"] = "false",
        });

        ServiceCollection services = new();

        // Act
        services.AddFoundationCachingRedis(configuration);

        // Assert — AesCacheValueEncryptor ne doit PAS être enregistré
        ServiceDescriptor? aesDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ICacheValueEncryptor)
                 && d.ImplementationType == typeof(AesCacheValueEncryptor));
        aesDescriptor.Should().BeNull();
    }

    [Fact]
    public void AddFoundationCachingRedis_ConfiguresRedisCachingOptions()
    {
        // Arrange
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Cache:Redis:IsEnabled"] = "true",
            ["Cache:Redis:Configuration"] = "redis-service:6379",
            ["Cache:Redis:InstanceName"] = "guava:",
        });

        ServiceCollection services = new();
        services.AddFoundationCachingRedis(configuration);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        RedisCachingOptions opts = sp.GetRequiredService<IOptions<RedisCachingOptions>>().Value;

        // Assert
        opts.Configuration.Should().Be("redis-service:6379");
        opts.InstanceName.Should().Be("guava:");
    }
}
