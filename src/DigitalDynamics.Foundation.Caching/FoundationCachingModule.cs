using DigitalDynamics.Foundation.Caching.Extensions;
using DigitalDynamics.Foundation.Core.Modularity;

namespace DigitalDynamics.Foundation.Caching;

/// <summary>
/// Module Foundation pour le cache distribué avec fournisseur Memory par défaut.
/// </summary>
/// <remarks>
/// Ce module configure l'abstraction de cache <see cref="ICacheService{TCacheItem}"/> avec
/// <c>MemoryDistributedCache</c> comme fournisseur. Idéal pour le développement et les tests.
/// <para>
/// Pour la production, remplacez par :
/// <list type="bullet">
///   <item><c>FoundationCachingRedisModule</c> — Redis (cohérence inter-pods)</item>
///   <item><c>FoundationCachingHybridModule</c> — L1+L2 (performance Kubernetes)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class FoundationCachingModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationCaching(
            context.Configuration.GetSection(CachingOptions.SectionName));
}
