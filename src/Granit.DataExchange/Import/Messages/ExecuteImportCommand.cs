namespace Granit.DataExchange.Import.Messages;

/// <summary>
/// Wolverine command to execute an import job in the background.
/// Published when the user confirms mappings and triggers execution.
/// </summary>
/// <param name="ImportJobId">The import job identifier.</param>
/// <param name="DefinitionName">The import definition name (for handler resolution).</param>
public sealed record ExecuteImportCommand(
    Guid ImportJobId,
    string DefinitionName);
