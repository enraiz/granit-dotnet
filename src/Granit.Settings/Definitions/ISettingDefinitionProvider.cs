namespace Granit.Settings.Definitions;

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
