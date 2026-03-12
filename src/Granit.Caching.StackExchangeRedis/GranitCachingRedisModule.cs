using Granit.Caching.StackExchangeRedis.Extensions;
using Granit.Caching.StackExchangeRedis.Options;
using Granit.Core.Modularity;
using Microsoft.Extensions.Configuration;

namespace Granit.Caching.StackExchangeRedis;

/// <summary>
/// Module Granit pour le fournisseur Redis du cache distribué.
/// Remplace le fournisseur Memory enregistré par <c>GranitCachingModule</c>.
/// </summary>
/// <remarks>
/// Ce module dépend de <c>GranitCachingModule</c> qui enregistre l'abstraction
/// <see cref="Granit.Caching.ICacheService{TCacheItem}"/> et les options.
/// <para>
/// Activation du chiffrement AES-256 :
/// <code>
/// // appsettings.json
/// {
///   "Cache": {
///     "EncryptValues": true,
///     "Encryption": { "Key": "base64-key-from-vault" },
///     "Redis": { "Configuration": "redis:6379", "InstanceName": "app:" }
///   }
/// }
/// </code>
/// </para>
/// </remarks>
[DependsOn(typeof(GranitCachingModule))]
public sealed class GranitCachingRedisModule : GranitModule
{
    /// <inheritdoc/>
    public override bool IsEnabled(ServiceConfigurationContext context)
    {
        RedisCachingOptions redisOpts = context.Configuration
            .GetSection(RedisCachingOptions.SectionName)
            .Get<RedisCachingOptions>() ?? new RedisCachingOptions();

        return redisOpts.IsEnabled;
    }

    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitCachingRedis();
}
