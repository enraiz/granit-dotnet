using DigitalDynamics.Foundation.Caching.Hybrid.Extensions;
using DigitalDynamics.Foundation.Caching.StackExchangeRedis;
using DigitalDynamics.Foundation.Core.Modularity;

namespace DigitalDynamics.Foundation.Caching.Hybrid;

/// <summary>
/// Module Foundation pour le fournisseur HybridCache (L1+L2, Kubernetes).
/// Combine un cache L1 en mémoire locale (par pod) et un cache L2 Redis partagé.
/// </summary>
/// <remarks>
/// Ce module dépend de <c>FoundationCachingRedisModule</c> (qui dépend lui-même de
/// <c>FoundationCachingModule</c>). L'ordre d'initialisation est garanti par
/// le système de modules Foundation.
/// <para>
/// Comportement d'invalidation inter-pods :
/// <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache"/> n'invalide pas les L1 distants.
/// Configurez <c>Cache:Hybrid:LocalCacheExpiration</c> à ≤ 60 s (défaut : 30 s)
/// pour borner la fenêtre de données obsolètes entre pods.
/// </para>
/// <para>
/// Configuration <c>appsettings.json</c> :
/// <code>
/// {
///   "Cache": {
///     "KeyPrefix": "guava",
///     "EncryptValues": true,
///     "Encryption": { "Key": "base64-key-from-vault" },
///     "Redis": { "Configuration": "redis:6379", "InstanceName": "guava:" },
///     "Hybrid": { "LocalCacheExpiration": "00:00:30" }
///   }
/// }
/// </code>
/// </para>
/// </remarks>
[DependsOn(typeof(FoundationCachingRedisModule))]
public sealed class FoundationCachingHybridModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationCachingHybrid(context.Configuration);
}
