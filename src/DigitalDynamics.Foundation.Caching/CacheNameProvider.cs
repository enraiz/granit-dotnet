using System.Collections.Concurrent;
using System.Reflection;

namespace DigitalDynamics.Foundation.Caching;

/// <summary>
/// Fournit le nom de cache d'un type selon la convention ou l'attribut <see cref="CacheNameAttribute"/>.
/// Les noms résolus sont mis en cache en mémoire pour éviter la réflexion répétée.
/// </summary>
/// <remarks>
/// Convention : le suffixe <c>"CacheItem"</c> est retiré du nom du type.
/// Exemples : <c>UserCacheItem</c> → <c>"User"</c>, <c>PatientRecord</c> → <c>"PatientRecord"</c>
/// Surcharge via <c>[CacheName("nom")]</c> sur la classe.
/// </remarks>
internal static class CacheNameProvider
{
    private static readonly ConcurrentDictionary<Type, string> _cache = new();
    private const string CacheItemSuffix = "CacheItem";

    /// <summary>
    /// Retourne le nom de cache pour le type spécifié.
    /// </summary>
    internal static string GetCacheName(Type type) =>
        _cache.GetOrAdd(type, ResolveNameFromType);

    private static string ResolveNameFromType(Type type)
    {
        CacheNameAttribute? attribute = type.GetCustomAttribute<CacheNameAttribute>();

        if (attribute is not null)
        {
            return attribute.Name;
        }

        string name = type.Name;

        return name.EndsWith(CacheItemSuffix, StringComparison.Ordinal)
            ? name[..^CacheItemSuffix.Length]
            : name;
    }
}
