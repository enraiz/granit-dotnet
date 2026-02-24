namespace Granit.Persistence.Migrations;

/// <summary>
/// Configuration options for <c>MigrationStartupService</c>.
/// Bound from the <c>"GranitMigrations"</c> section in <c>appsettings.json</c>.
/// </summary>
public sealed class MigrationStartupOptions
{
    /// <summary>
    /// Configuration section name in <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "GranitMigrations";

    /// <summary>
    /// Default number of rows to process per batch.
    /// Used when resuming pending cycles at startup.
    /// Defaults to <c>500</c>.
    /// </summary>
    public int DefaultBatchSize { get; set; } = 500;
}
