namespace Granit.ReferenceData;

/// <summary>
/// Configuration options for the reference data module.
/// Bind to the <c>"ReferenceData"</c> configuration section.
/// </summary>
public sealed class ReferenceDataOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ReferenceData";

    /// <summary>
    /// Duration for which reference data entries are cached in memory.
    /// Default: 1 hour. Reference data changes rarely, so a long TTL is appropriate.
    /// </summary>
    public TimeSpan CacheTimeToLive { get; set; } = TimeSpan.FromHours(1);
}
