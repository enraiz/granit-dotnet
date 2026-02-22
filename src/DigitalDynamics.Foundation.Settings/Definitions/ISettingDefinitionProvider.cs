// =============================================================================
// ISettingDefinitionProvider - Extension point for declaring settings
// =============================================================================
// Implement this interface in each module to declare its settings.
// Register with services.AddSettingDefinitionProvider<T>().
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Definitions;

/// <summary>
/// Extension point allowing a module to declare its settings.
/// </summary>
public interface ISettingDefinitionProvider
{
    /// <summary>
    /// Declares the module's settings via <paramref name="context"/>.
    /// </summary>
    void Define(ISettingDefinitionContext context);
}
