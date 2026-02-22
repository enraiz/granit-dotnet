// =============================================================================
// SettingDefinitionManager - Registre centralisé des définitions de paramètres
// =============================================================================
// Singleton. Au démarrage, appelle Define() sur tous les ISettingDefinitionProvider
// enregistrés et consolide les définitions dans un dictionnaire immuable.
//
// Inputs  : IEnumerable<ISettingDefinitionProvider>
// Outputs : SettingDefinition par nom (Get / GetOrNull)
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Definitions;

/// <summary>
/// Registre centralisé de toutes les définitions de paramètres déclarées par les modules.
/// </summary>
public sealed class SettingDefinitionManager
{
    private readonly IReadOnlyDictionary<string, SettingDefinition> _definitions;

    public SettingDefinitionManager(IEnumerable<ISettingDefinitionProvider> providers)
    {
        SettingDefinitionContext context = new();
        foreach (ISettingDefinitionProvider provider in providers)
        {
            provider.Define(context);
        }
        _definitions = context.Build();
    }

    /// <summary>
    /// Retourne la définition pour le nom donné.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si le paramètre n'est pas déclaré.</exception>
    public SettingDefinition Get(string name) =>
        _definitions.TryGetValue(name, out SettingDefinition? def)
            ? def
            : throw new InvalidOperationException($"Le paramètre '{name}' n'est pas déclaré. Vérifiez qu'un ISettingDefinitionProvider l'enregistre.");

    /// <summary>Retourne la définition ou <c>null</c> si inconnue.</summary>
    public SettingDefinition? GetOrNull(string name) =>
        _definitions.GetValueOrDefault(name);

    /// <summary>Retourne toutes les définitions déclarées.</summary>
    public IReadOnlyCollection<SettingDefinition> GetAll() =>
        (IReadOnlyCollection<SettingDefinition>)_definitions.Values;

    private sealed class SettingDefinitionContext : ISettingDefinitionContext
    {
        private readonly Dictionary<string, SettingDefinition> _defs = [];

        public void Add(SettingDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            _defs[definition.Name] = definition;
        }

        public SettingDefinition? GetOrNull(string name) =>
            _defs.GetValueOrDefault(name);

        public System.Collections.ObjectModel.ReadOnlyDictionary<string, SettingDefinition> Build() =>
            _defs.AsReadOnly();
    }
}
