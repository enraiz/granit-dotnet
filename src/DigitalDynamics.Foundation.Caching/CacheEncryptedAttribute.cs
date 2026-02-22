namespace DigitalDynamics.Foundation.Caching;

/// <summary>
/// Contrôle finement le chiffrement AES-256 des valeurs pour ce type de cache.
/// Prioritaire sur le flag global <see cref="CachingOptions.EncryptValues"/>.
/// </summary>
/// <remarks>
/// Logique de priorité :
/// <list type="table">
///   <listheader><term>Attribut</term><term>EncryptValues global</term><term>Résultat</term></listheader>
///   <item><term><c>[CacheEncrypted]</c></term><term>peu importe</term><term>Chiffrement ACTIVÉ</term></item>
///   <item><term><c>[CacheEncrypted(false)]</c></term><term>peu importe</term><term>Chiffrement DÉSACTIVÉ</term></item>
///   <item><term>Pas d'attribut</term><term><c>true</c></term><term>Chiffrement ACTIVÉ</term></item>
///   <item><term>Pas d'attribut</term><term><c>false</c></term><term>Chiffrement DÉSACTIVÉ</term></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Toujours chiffré — données médicales HDS
/// [CacheEncrypted]
/// public sealed class PatientCacheItem { ... }
///
/// // Jamais chiffré — opt-out explicite (données de configuration publiques)
/// [CacheEncrypted(false)]
/// public sealed class AppConfigCacheItem { ... }
///
/// // Suit le flag global CachingOptions.EncryptValues
/// public sealed class UserPreferencesCacheItem { ... }
/// </code>
/// </example>
/// <param name="encrypt"><c>true</c> (défaut) pour forcer le chiffrement, <c>false</c> pour l'interdire.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheEncryptedAttribute(bool encrypt = true) : Attribute
{
    /// <summary><c>true</c> pour forcer le chiffrement, <c>false</c> pour le désactiver.</summary>
    public bool Encrypt { get; } = encrypt;
}
