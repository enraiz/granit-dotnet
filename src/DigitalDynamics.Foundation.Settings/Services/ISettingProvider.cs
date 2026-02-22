// =============================================================================
// ISettingProvider - Service de lecture des paramètres avec cascade automatique
// =============================================================================
// Parcourt la chaîne de providers U → T → G → C → D et retourne la première
// valeur non nulle. Gère l'héritage (IsInherited) et la liste blanche (Providers).
//
// Usage : injecter ISettingProvider en lecture seule.
//         Pour écrire, utiliser ISettingManager.
// =============================================================================

using DigitalDynamics.Foundation.Settings.Values;

namespace DigitalDynamics.Foundation.Settings.Services;

/// <summary>
/// Service de lecture des paramètres avec résolution en cascade automatique.
/// </summary>
public interface ISettingProvider
{
    /// <summary>
    /// Retourne la valeur résolue du paramètre, ou <c>null</c> si aucun provider ne fournit de valeur.
    /// </summary>
    /// <param name="name">Nom du paramètre.</param>
    /// <param name="ct">Token d'annulation.</param>
    Task<string?> GetOrNullAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Retourne les valeurs résolues pour une liste de paramètres.
    /// </summary>
    /// <param name="names">Noms des paramètres à résoudre.</param>
    /// <param name="ct">Token d'annulation.</param>
    Task<IReadOnlyList<SettingValue>> GetAllAsync(string[] names, CancellationToken ct = default);
}
