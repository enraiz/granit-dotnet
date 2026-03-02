namespace Granit.DataImport.Domain;

/// <summary>
/// Status of an import job throughout its lifecycle.
/// </summary>
public enum ImportJobStatus
{
    /// <summary>File uploaded, job created.</summary>
    Created = 0,

    /// <summary>Headers extracted, preview and mapping suggestions generated.</summary>
    Previewed = 1,

    /// <summary>Column mappings confirmed by the user.</summary>
    Mapped = 2,

    /// <summary>Import is currently executing (Wolverine background handler).</summary>
    Executing = 3,

    /// <summary>All rows imported successfully.</summary>
    Completed = 4,

    /// <summary>Some rows failed but others succeeded.</summary>
    PartiallyCompleted = 5,

    /// <summary>Import failed entirely.</summary>
    Failed = 6,

    /// <summary>Import was cancelled by the user.</summary>
    Cancelled = 7,
}
