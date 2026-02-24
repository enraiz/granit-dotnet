namespace Granit.Localization.DatabaseSource;

/// <summary>
/// Configuration options for the localization DB override cache.
/// </summary>
public sealed class LocalizationDatabaseSourceOptions
{
    /// <summary>
    /// Duration for which override dictionaries are kept in memory before being reloaded.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);
}
