namespace Granit.Localization.Options;

/// <summary>
/// Configuration options for the localization DB override cache.
/// </summary>
public sealed class LocalizationOverridesCacheOptions
{
    /// <summary>
    /// Duration for which override dictionaries are kept in memory before being reloaded.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);
}
