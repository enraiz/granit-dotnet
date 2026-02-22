// =============================================================================
// SettingValueProviderManager - Gestionnaire ordonné des providers de valeur
// =============================================================================
// Agrège tous les ISettingValueProvider enregistrés dans le DI et les trie
// par ordre croissant (100 = plus prioritaire, 500 = dernier recours).
//
// Ordre par défaut :
//   User (U, 100) → Tenant (T, 200) → Global (G, 300)
//   → Configuration (C, 400) → Default (D, 500)
//
// Inputs  : IEnumerable<ISettingValueProvider> (DI)
// Outputs : Providers triés (accès par liste ou par nom)
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Gestionnaire des providers de valeur de paramètre, triés par ordre de priorité.
/// </summary>
public sealed class SettingValueProviderManager
{
    /// <summary>
    /// Liste ordonnée des providers, du plus prioritaire (U=100) au moins prioritaire (D=500).
    /// </summary>
    public IReadOnlyList<ISettingValueProvider> Providers { get; }

    public SettingValueProviderManager(IEnumerable<ISettingValueProvider> providers) =>
        Providers = [.. providers.OrderBy(p => p.Order)];

    /// <summary>
    /// Retourne le provider par son nom court, ou <c>null</c> s'il n'est pas enregistré.
    /// </summary>
    public ISettingValueProvider? GetOrNull(string name) =>
        Providers.FirstOrDefault(p => p.Name == name);
}
