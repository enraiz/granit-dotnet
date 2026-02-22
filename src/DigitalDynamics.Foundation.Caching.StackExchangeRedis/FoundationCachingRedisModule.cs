using DigitalDynamics.Foundation.Caching.StackExchangeRedis.Extensions;
using DigitalDynamics.Foundation.Core.Modularity;

namespace DigitalDynamics.Foundation.Caching.StackExchangeRedis;

/// <summary>
/// Module Foundation pour le fournisseur Redis du cache distribué.
/// Remplace le fournisseur Memory enregistré par <c>FoundationCachingModule</c>.
/// </summary>
/// <remarks>
/// Ce module dépend de <c>FoundationCachingModule</c> qui enregistre l'abstraction
/// <see cref="DigitalDynamics.Foundation.Caching.ICacheService{TCacheItem}"/> et les options.
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
[DependsOn(typeof(FoundationCachingModule))]
public sealed class FoundationCachingRedisModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationCachingRedis(context.Configuration);
}
