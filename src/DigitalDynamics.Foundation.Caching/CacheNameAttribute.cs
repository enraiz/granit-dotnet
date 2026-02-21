namespace DigitalDynamics.Foundation.Caching;

/// <summary>
/// Surcharge le nom de cache calculé par convention pour ce type.
/// Par convention, <c>UserCacheItem</c> → <c>"User"</c> (suffixe "CacheItem" retiré).
/// Utilisez cet attribut pour personnaliser le nom et donc la partie centrale de la clé composite.
/// </summary>
/// <example>
/// <code>
/// [CacheName("Patient")]
/// public sealed class PatientSummaryCacheItem { }
/// // Clé générée : dd:Patient:{userKey}
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheNameAttribute : Attribute
{
    /// <summary>Nom de cache personnalisé.</summary>
    public string Name { get; }

    /// <param name="name">Nom à utiliser comme segment central de la clé composite.</param>
    public CacheNameAttribute(string name) => Name = name;
}
