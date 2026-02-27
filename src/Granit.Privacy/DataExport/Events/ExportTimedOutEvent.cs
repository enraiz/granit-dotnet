namespace Granit.Privacy.DataExport.Events;

/// <summary>
/// Internal timeout event for the GDPR export Saga.
/// Scheduled when the Saga starts; triggers partial export completion if not all fragments arrived.
/// </summary>
public sealed record ExportTimedOutEvent(Guid RequestId);
