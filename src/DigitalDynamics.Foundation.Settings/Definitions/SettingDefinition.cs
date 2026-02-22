// =============================================================================
// SettingDefinition - Déclaration statique d'un paramètre
// =============================================================================
// Décrit un paramètre : nom, valeur par défaut, chiffrement, visibilité,
// héritage et liste blanche des providers autorisés.
//
// Créé par ISettingDefinitionProvider.Define() au démarrage.
// Géré par SettingDefinitionManager (Singleton).
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Definitions;

/// <summary>
/// Décrit un paramètre du système (métadonnées statiques).
/// </summary>
public sealed class SettingDefinition
{
    /// <summary>Nom unique du paramètre (clé de lookup).</summary>
    public string Name { get; }

    /// <summary>Valeur par défaut retournée si aucun provider ne fournit de valeur.</summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Indique si la valeur doit être chiffrée au repos (via IStringEncryptionService).
    /// Le cache stocke le plaintext — le chiffrement s'applique uniquement à la couche store.
    /// </summary>
    public bool IsEncrypted { get; init; }

    /// <summary>Si vrai, la valeur est exposable aux clients (API publique).</summary>
    public bool IsVisibleToClients { get; init; }

    /// <summary>
    /// Si vrai (défaut), le provider de rang inférieur hérite la valeur du rang supérieur
    /// quand la sienne est nulle. Ex : Tenant hérite Global si IsInherited = true.
    /// </summary>
    public bool IsInherited { get; init; } = true;

    /// <summary>
    /// Liste blanche des noms de providers autorisés à stocker ce paramètre.
    /// Liste vide = tous les providers sont autorisés.
    /// </summary>
    public IList<string> Providers { get; } = [];

    /// <summary>Libellé d'affichage (UI).</summary>
    public string? DisplayName { get; init; }

    /// <summary>Description longue du paramètre (UI, documentation).</summary>
    public string? Description { get; init; }

    public SettingDefinition(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
