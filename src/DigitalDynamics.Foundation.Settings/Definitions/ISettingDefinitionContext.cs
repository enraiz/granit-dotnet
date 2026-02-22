// =============================================================================
// ISettingDefinitionContext - Setting declaration context
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Definitions;

/// <summary>
/// Context passed to <see cref="ISettingDefinitionProvider.Define"/>
/// to register or query setting definitions.
/// </summary>
public interface ISettingDefinitionContext
{
    /// <summary>Adds a setting definition.</summary>
    /// <param name="definition">The definition to register.</param>
    void Add(SettingDefinition definition);

    /// <summary>
    /// Returns the definition associated with the name, or <c>null</c> if unknown.
    /// </summary>
    SettingDefinition? GetOrNull(string name);
}
