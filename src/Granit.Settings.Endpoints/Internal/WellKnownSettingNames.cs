namespace Granit.Settings.Endpoints.Internal;

/// <summary>
/// Well-known setting names declared by the settings endpoints module.
/// </summary>
/// <remarks>
/// Public so that application modules (e.g. Keycloak sync filters) can reference
/// the canonical setting names without duplicating string constants.
/// </remarks>
public static class WellKnownSettingNames
{
    /// <summary>Preferred locale (BCP 47 language tag, e.g. "fr", "en-GB").</summary>
    public const string PreferredCulture = "Granit.Localization.PreferredCulture";

    /// <summary>Preferred timezone (IANA identifier, e.g. "Europe/Brussels").</summary>
    public const string PreferredTimezone = "Granit.Timing.PreferredTimezone";
}
