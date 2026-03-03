namespace Granit.DataImport.Export;

/// <summary>
/// Status of an export job throughout its lifecycle.
/// </summary>
public enum ExportJobStatus
{
    /// <summary>Job created and queued for background execution.</summary>
    Queued = 0,

    /// <summary>Export is currently being generated.</summary>
    Exporting = 1,

    /// <summary>Export completed successfully — file available for download.</summary>
    Completed = 2,

    /// <summary>Export failed.</summary>
    Failed = 3,
}
