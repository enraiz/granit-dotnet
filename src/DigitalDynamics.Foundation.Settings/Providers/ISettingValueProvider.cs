// =============================================================================
// ISettingValueProvider - Abstraction d'un provider de valeur de paramètre
// =============================================================================
// Chaque provider représente un niveau dans la cascade de résolution :
//   User (U, order=100) → Tenant (T, 200) → Global (G, 300)
//   → Configuration (C, 400) → Default (D, 500)
//
// Les providers de store (G, T, U) utilisent ISettingStore + ICacheService<SettingValue>.
// Les providers statiques (C, D) lisent IConfiguration ou SettingDefinition.DefaultValue.
// =============================================================================

using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Values;

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Fournit la valeur d'un paramètre pour un niveau donné dans la cascade de résolution.
/// </summary>
public interface ISettingValueProvider
{
    /// <summary>Identifiant court du provider (ex : "G", "T", "U", "C", "D").</summary>
    string Name { get; }

    /// <summary>Ordre de résolution (priorité croissante : 100 = plus prioritaire).</summary>
    int Order { get; }

    /// <summary>
    /// Retourne la valeur du paramètre pour le contexte courant, ou <c>null</c> si absente.
    /// </summary>
    Task<SettingValue?> GetOrNullAsync(SettingDefinition definition, CancellationToken ct = default);

    /// <summary>
    /// Crée ou met à jour la valeur du paramètre pour le contexte courant.
    /// </summary>
    Task SetAsync(SettingDefinition definition, string? value, CancellationToken ct = default);

    /// <summary>
    /// Supprime la valeur du paramètre pour le contexte courant.
    /// </summary>
    Task ClearAsync(SettingDefinition definition, CancellationToken ct = default);
}
