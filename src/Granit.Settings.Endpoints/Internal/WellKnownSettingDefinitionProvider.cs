using Granit.Settings.Definitions;

namespace Granit.Settings.Endpoints.Internal;

/// <summary>
/// Declares the built-in user preference settings (locale, timezone).
/// </summary>
internal sealed class WellKnownSettingDefinitionProvider : ISettingDefinitionProvider
{
    /// <inheritdoc />
    public void Define(ISettingDefinitionContext context)
    {
        context.Add(new SettingDefinition(WellKnownSettingNames.PreferredCulture)
        {
            IsVisibleToClients = true,
            DisplayName = "Preferred culture",
            Description = "BCP 47 language tag for the user's preferred locale (e.g. fr, en-GB).",
            Providers = { "U", "T", "G" },
        });

        context.Add(new SettingDefinition(WellKnownSettingNames.PreferredTimezone)
        {
            IsVisibleToClients = true,
            DisplayName = "Preferred timezone",
            Description = "IANA timezone identifier for the user's preferred timezone (e.g. Europe/Brussels).",
            Providers = { "U", "T", "G" },
        });
    }
}
