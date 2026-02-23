using Granit.Caching.Extensions;
using Granit.Core.Modularity;

namespace Granit.Caching;

/// <summary>
/// Module Granit pour le cache distribué avec fournisseur Memory par défaut.
/// </summary>
/// <remarks>
/// Ce module configure l'abstraction de cache <see cref="ICacheService{TCacheItem}"/> avec
/// <c>MemoryDistributedCache</c> comme fournisseur. Idéal pour le développement et les tests.
/// <para>
/// Pour la production, remplacez par :
/// <list type="bullet">
///   <item><c>GranitCachingRedisModule</c> — Redis (cohérence inter-pods)</item>
///   <item><c>GranitCachingHybridModule</c> — L1+L2 (performance Kubernetes)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class GranitCachingModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitCaching(
            context.Configuration.GetSection(CachingOptions.SectionName));
}
