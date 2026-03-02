namespace Granit.DataImport.Messages;

/// <summary>
/// Wolverine event published when an import job finishes (success, partial, or failure).
/// Can be consumed by notification handlers, audit loggers, etc.
/// </summary>
/// <param name="ImportJobId">The import job identifier.</param>
/// <param name="DefinitionName">The import definition name.</param>
/// <param name="TotalRows">Total rows processed.</param>
/// <param name="SucceededRows">Rows successfully imported.</param>
/// <param name="FailedRows">Rows that failed.</param>
public sealed record ImportCompletedEvent(
    Guid ImportJobId,
    string DefinitionName,
    int TotalRows,
    int SucceededRows,
    int FailedRows);
