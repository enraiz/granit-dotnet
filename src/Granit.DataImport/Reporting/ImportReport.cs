using Granit.DataImport.Domain;

namespace Granit.DataImport.Reporting;

/// <summary>
/// Final report of an import execution.
/// Only errored rows are stored — successful rows are counted but not kept in memory.
/// </summary>
/// <remarks>
/// For 100K rows with 50 errors, only ~50 <see cref="ImportRowError"/> objects are in memory.
/// </remarks>
public sealed class ImportReport
{
    /// <summary>Total number of rows processed.</summary>
    public required int TotalRows { get; init; }

    /// <summary>Number of rows successfully imported.</summary>
    public required int SucceededRows { get; init; }

    /// <summary>Number of rows that failed (conversion, validation, or persistence).</summary>
    public required int FailedRows { get; init; }

    /// <summary>Number of rows skipped (e.g. empty rows).</summary>
    public required int SkippedRows { get; init; }

    /// <summary>Number of new records inserted.</summary>
    public required int InsertedRows { get; init; }

    /// <summary>Number of existing records updated.</summary>
    public required int UpdatedRows { get; init; }

    /// <summary>Total elapsed time for the import execution.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Final status of the import job.</summary>
    public required ImportJobStatus FinalStatus { get; init; }

    /// <summary>
    /// Row-level errors. Only rows that failed are included — not the successful ones.
    /// </summary>
    public required IReadOnlyList<ImportRowError> RowErrors { get; init; }
}
