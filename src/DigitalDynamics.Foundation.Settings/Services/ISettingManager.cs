// =============================================================================
// ISettingManager - Service d'écriture des paramètres
// =============================================================================
// Permet d'écrire des valeurs de paramètres à un niveau précis de la cascade
// (Global, Tenant, User) sans dépendre du contexte courant.
//
// Pour la lecture avec cascade automatique, utiliser ISettingProvider.
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Services;

/// <summary>
/// Service d'écriture des paramètres pour les portées Global, Tenant et User.
/// </summary>
public interface ISettingManager
{
    /// <summary>Définit la valeur d'un paramètre au niveau global.</summary>
    Task SetGlobalAsync(string name, string? value, CancellationToken ct = default);

    /// <summary>Définit la valeur d'un paramètre pour un tenant spécifique.</summary>
    Task SetForTenantAsync(Guid tenantId, string name, string? value, CancellationToken ct = default);

    /// <summary>Définit la valeur d'un paramètre pour un utilisateur spécifique.</summary>
    Task SetForUserAsync(string userId, string name, string? value, CancellationToken ct = default);

    /// <summary>
    /// Supprime la valeur d'un paramètre pour un provider et une clé donnés.
    /// </summary>
    /// <param name="name">Nom du paramètre.</param>
    /// <param name="providerName">Nom du provider ("G", "T", "U").</param>
    /// <param name="providerKey">Clé du provider (null = global, tenantId, userId).</param>
    /// <param name="ct">Token d'annulation.</param>
    Task DeleteAsync(
        string name,
        string providerName,
        string? providerKey = null,
        CancellationToken ct = default);
}
