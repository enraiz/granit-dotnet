// =============================================================================
// ISettingStore - Abstraction de persistance des valeurs de paramètres
// =============================================================================
// Interface basse couche : pas de cache, pas de chiffrement applicatif.
// Le chiffrement des valeurs IsEncrypted est géré à cette couche (store EF).
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Values;

/// <summary>
/// Abstraction de persistance des valeurs de paramètres.
/// Le chiffrement des paramètres sensibles est géré par l'implémentation.
/// </summary>
public interface ISettingStore
{
    /// <summary>
    /// Retourne la valeur stockée, ou <c>null</c> si absente.
    /// </summary>
    Task<SettingValue?> GetOrNullAsync(
        string name,
        string providerName,
        string? providerKey,
        CancellationToken ct = default);

    /// <summary>
    /// Retourne toutes les valeurs stockées pour un provider et une clé donnés.
    /// </summary>
    Task<IReadOnlyList<SettingValue>> GetListAsync(
        string providerName,
        string? providerKey,
        CancellationToken ct = default);

    /// <summary>
    /// Crée ou met à jour la valeur d'un paramètre.
    /// </summary>
    Task SetAsync(
        string name,
        string providerName,
        string? providerKey,
        string? value,
        CancellationToken ct = default);

    /// <summary>
    /// Supprime la valeur d'un paramètre pour un provider et une clé donnés.
    /// </summary>
    Task DeleteAsync(
        string name,
        string providerName,
        string? providerKey,
        CancellationToken ct = default);
}
