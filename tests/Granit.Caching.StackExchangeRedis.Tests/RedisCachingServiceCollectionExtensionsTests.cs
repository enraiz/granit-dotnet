// =============================================================================
// Tests - RedisCachingServiceCollectionExtensions
// =============================================================================
// Vérifie que AddGranitCachingRedis :
//   - Remplace IDistributedCache par RedisCache quand IsEnabled = true
//   - Ne modifie pas les services quand IsEnabled = false
//   - Enregistre AesCacheValueEncryptor si EncryptValues = true
//   - Conserve NullCacheValueEncryptor si EncryptValues = false
// =============================================================================

using FluentAssertions;
using Granit.Caching.StackExchangeRedis.Extensions;
using Granit.Caching.StackExchangeRedis.HealthChecks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;
using Xunit;

namespace Granit.Caching.StackExchangeRedis.Tests;

public sealed class RedisCachingServiceCollectionExtensionsTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void AddGranitCachingRedis_IsEnabledFalse_DoesNotReplaceDistributedCache()
    {
        // Arrange
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Cache:Redis:IsEnabled"] = "false",
        });

        ServiceCollection services = new();
        services.AddDistributedMemoryCache(); // Memory par défaut

        // Act
        services.AddGranitCachingRedis(configuration);

        // Assert — le MemoryDistributedCache doit rester
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IDistributedCache));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().NotBe<RedisCache>();
    }

    [Fact]
    public void AddGranitCachingRedis_IsEnabledTrue_RegistersRedisCache()
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
        services.AddGranitCachingRedis(configuration);

        // Assert — RedisCache doit être enregistré pour IDistributedCache
        ServiceDescriptor? redisDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IDistributedCache));
        redisDescriptor.Should().NotBeNull("Redis doit être enregistré comme IDistributedCache");
    }

    [Fact]
    public void AddGranitCachingRedis_EncryptValues_True_RegistersAesEncryptor()
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
        services.AddGranitCachingRedis(configuration);

        // Assert
        ServiceDescriptor? encryptorDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ICacheValueEncryptor));
        encryptorDescriptor.Should().NotBeNull();
        encryptorDescriptor!.ImplementationType.Should().Be<AesCacheValueEncryptor>();
    }

    [Fact]
    public void AddGranitCachingRedis_EncryptValues_False_DoesNotRegisterAesEncryptor()
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
        services.AddGranitCachingRedis(configuration);

        // Assert — AesCacheValueEncryptor ne doit PAS être enregistré
        ServiceDescriptor? aesDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ICacheValueEncryptor)
                 && d.ImplementationType == typeof(AesCacheValueEncryptor));
        aesDescriptor.Should().BeNull();
    }

    [Fact]
    public void AddGranitCachingRedis_ConfiguresRedisCachingOptions()
    {
        // Arrange
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Cache:Redis:IsEnabled"] = "true",
            ["Cache:Redis:Configuration"] = "redis-service:6379",
            ["Cache:Redis:InstanceName"] = "guava:",
        });

        ServiceCollection services = new();
        services.AddGranitCachingRedis(configuration);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        RedisCachingOptions opts = sp.GetRequiredService<IOptions<RedisCachingOptions>>().Value;

        // Assert
        opts.Configuration.Should().Be("redis-service:6379");
        opts.InstanceName.Should().Be("guava:");
    }

    [Fact]
    public void AddGranitCachingRedis_IsEnabledTrue_AppliesRedisConfigurationToStackExchangeOptions()
    {
        // Arrange — vérifie que le lambda AddStackExchangeRedisCache est bien exécuté
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Cache:Redis:IsEnabled"] = "true",
            ["Cache:Redis:Configuration"] = "redis-service:6379",
            ["Cache:Redis:InstanceName"] = "myapp:",
        });

        ServiceCollection services = new();
        services.AddGranitCachingRedis(configuration);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act — résoudre IOptions<RedisCacheOptions> force l'exécution du lambda de configuration
        RedisCacheOptions redisOpts = sp.GetRequiredService<IOptions<RedisCacheOptions>>().Value;

        // Assert
        redisOpts.Configuration.Should().Be("redis-service:6379");
        redisOpts.InstanceName.Should().Be("myapp:");
    }

    [Fact]
    public void AddGranitRedisCheck_WhenIConnectionMultiplexerNotRegistered_RegistersIt()
    {
        // Arrange
        ServiceCollection services = new();
        // Configure RedisCachingOptions so the factory lambda can read it
        services.Configure<RedisCachingOptions>(opts => opts.Configuration = "localhost:6379");
        IHealthChecksBuilder builder = services.AddHealthChecks();

        // Act
        builder.AddGranitRedisCheck();

        // Assert — IConnectionMultiplexer must have been registered
        ServiceDescriptor? multiplexerDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IConnectionMultiplexer));
        multiplexerDescriptor.Should().NotBeNull();
        multiplexerDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddGranitRedisCheck_WhenIConnectionMultiplexerAlreadyRegistered_DoesNotRegisterAgain()
    {
        // Arrange
        ServiceCollection services = new();
        IConnectionMultiplexer existingMultiplexer = Substitute.For<IConnectionMultiplexer>();
        services.AddSingleton(existingMultiplexer);
        IHealthChecksBuilder builder = services.AddHealthChecks();

        // Act
        builder.AddGranitRedisCheck();

        // Assert — only one IConnectionMultiplexer registration
        IEnumerable<ServiceDescriptor> multiplexerDescriptors = services.Where(
            d => d.ServiceType == typeof(IConnectionMultiplexer));
        multiplexerDescriptors.Should().HaveCount(1);
    }

    [Fact]
    public void AddGranitRedisCheck_RegistersCheckTaggedReadiness()
    {
        // Arrange
        ServiceCollection services = new();
        IConnectionMultiplexer existingMultiplexer = Substitute.For<IConnectionMultiplexer>();
        services.AddSingleton(existingMultiplexer);
        IHealthChecksBuilder builder = services.AddHealthChecks();

        // Act
        builder.AddGranitRedisCheck(name: "redis");

        // Assert — HealthCheckRegistration tagged "readiness"
        using ServiceProvider sp = services.BuildServiceProvider();
        HealthCheckServiceOptions opts = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        HealthCheckRegistration? registration = opts.Registrations.FirstOrDefault(r => r.Name == "redis");
        registration.Should().NotBeNull();
        registration!.Tags.Should().Contain("readiness");
    }
}
