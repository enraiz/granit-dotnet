// =============================================================================
// ISettingDefinitionProvider - Point d'extension pour déclarer des paramètres
// =============================================================================
// Implémenter cette interface dans chaque module pour déclarer ses paramètres.
// Enregistrer avec services.AddSettingDefinitionProvider<T>().
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Definitions;

/// <summary>
/// Point d'extension permettant à un module de déclarer ses paramètres.
/// </summary>
public interface ISettingDefinitionProvider
{
    /// <summary>
    /// Déclare les paramètres du module via <paramref name="context"/>.
    /// </summary>
    void Define(ISettingDefinitionContext context);
}
