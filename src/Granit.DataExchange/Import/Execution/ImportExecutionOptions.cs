namespace Granit.DataExchange.Import.Execution;

/// <summary>
/// Options controlling how the import is executed.
/// </summary>
public sealed class ImportExecutionOptions
{
    /// <summary>
    /// Number of entities to process per batch before calling <c>SaveChanges</c>.
    /// Default: <c>500</c>.
    /// </summary>
    public int BatchSize { get; init; } = 500;

    /// <summary>
    /// When <c>true</c>, executes the import within a savepoint and rolls back
    /// after validation — no data is persisted.
    /// </summary>
    public bool DryRun { get; init; }

    /// <summary>
    /// How to handle errors during import.
    /// Default: <see cref="ImportErrorBehavior.SkipErrors"/>.
    /// </summary>
    public ImportErrorBehavior ErrorBehavior { get; init; } = ImportErrorBehavior.SkipErrors;
}
