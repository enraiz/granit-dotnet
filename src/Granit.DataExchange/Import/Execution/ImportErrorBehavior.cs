namespace Granit.DataExchange.Import.Execution;

/// <summary>
/// Determines how import errors are handled during execution.
/// </summary>
public enum ImportErrorBehavior
{
    /// <summary>Stop immediately on the first error.</summary>
    FailFast,

    /// <summary>
    /// Skip errored rows and continue processing. Default behavior.
    /// Errored rows are recorded in the <see cref="Reporting.ImportReport"/>.
    /// </summary>
    SkipErrors,

    /// <summary>
    /// Process all rows and collect all errors without stopping.
    /// Similar to <see cref="SkipErrors"/> but signals intent to review all errors.
    /// </summary>
    CollectAll,
}
