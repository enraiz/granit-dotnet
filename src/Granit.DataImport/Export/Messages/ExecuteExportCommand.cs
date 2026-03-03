namespace Granit.DataImport.Export.Messages;

/// <summary>
/// Command to execute an export job in the background.
/// Published when the export is dispatched to a background worker.
/// </summary>
/// <param name="ExportJobId">The export job identifier.</param>
public sealed record ExecuteExportCommand(Guid ExportJobId);
