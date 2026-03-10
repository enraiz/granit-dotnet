using System.Reflection;
using Granit.Caching.Options;

namespace Granit.Caching;

/// <summary>
/// Résout si le chiffrement AES doit être appliqué pour un type donné.
/// Prend en compte l'attribut <see cref="CacheEncryptedAttribute"/> et le flag global <see cref="CachingOptions.EncryptValues"/>.
/// </summary>
internal static class CacheEncryptionResolver
{
    /// <summary>
    /// Détermine si les valeurs du type <paramref name="type"/> doivent être chiffrées.
    /// </summary>
    /// <param name="type">Type du cache item.</param>
    /// <param name="options">Options globales de cache.</param>
    /// <returns><c>true</c> si le chiffrement doit être appliqué.</returns>
    internal static bool ShouldEncrypt(Type type, CachingOptions options)
    {
        CacheEncryptedAttribute? attribute = type.GetCustomAttribute<CacheEncryptedAttribute>();

        return attribute is not null
            ? attribute.Encrypt
            : options.EncryptValues;
    }
}
