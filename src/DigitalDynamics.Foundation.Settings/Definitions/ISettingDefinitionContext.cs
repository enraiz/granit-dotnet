// =============================================================================
// ISettingDefinitionContext - Contexte de déclaration des paramètres
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Definitions;

/// <summary>
/// Contexte passé à <see cref="ISettingDefinitionProvider.Define"/>
/// pour enregistrer ou consulter des définitions de paramètres.
/// </summary>
public interface ISettingDefinitionContext
{
    /// <summary>Ajoute une définition de paramètre.</summary>
    /// <param name="definition">La définition à enregistrer.</param>
    void Add(SettingDefinition definition);

    /// <summary>
    /// Retourne la définition associée au nom, ou <c>null</c> si inconnue.
    /// </summary>
    SettingDefinition? GetOrNull(string name);
}
