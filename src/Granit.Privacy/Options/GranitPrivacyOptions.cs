namespace Granit.Privacy.Options;

/// <summary>
/// Configuration options for the Granit.Privacy module.
/// </summary>
public sealed class GranitPrivacyOptions
{
    /// <summary>Section key in the configuration.</summary>
    public const string SectionName = "Privacy";

    /// <summary>
    /// Timeout in minutes for the GDPR export Saga.
    /// If not all providers respond within this time, a partial export is generated.
    /// Default: 5 minutes.
    /// </summary>
    public int ExportTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum size in megabytes for the export archive.
    /// Default: 100 MB.
    /// </summary>
    public int ExportMaxSizeMb { get; set; } = 100;
}
