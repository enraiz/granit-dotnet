namespace Granit.Settings.Endpoints.Internal;

/// <summary>
/// Well-known setting names declared by the settings endpoints module.
/// </summary>
internal static class WellKnownSettingNames
{
    /// <summary>Preferred locale (BCP 47 language tag, e.g. "fr", "en-GB").</summary>
    internal const string PreferredCulture = "Granit.Localization.PreferredCulture";

    /// <summary>Preferred timezone (IANA identifier, e.g. "Europe/Brussels").</summary>
    internal const string PreferredTimezone = "Granit.Timing.PreferredTimezone";
}
