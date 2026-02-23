using Granit.Caching.Hybrid.Extensions;
using Granit.Caching.StackExchangeRedis;
using Granit.Core.Modularity;

namespace Granit.Caching.Hybrid;

/// <summary>
/// Module Granit pour le fournisseur HybridCache (L1+L2, Kubernetes).
/// Combine un cache L1 en mémoire locale (par pod) et un cache L2 Redis partagé.
/// </summary>
/// <remarks>
/// Ce module dépend de <c>GranitCachingRedisModule</c> (qui dépend lui-même de
/// <c>GranitCachingModule</c>). L'ordre d'initialisation est garanti par
/// le système de modules Granit.
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
[DependsOn(typeof(GranitCachingRedisModule))]
public sealed class GranitCachingHybridModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitCachingHybrid(context.Configuration);
}
