namespace Granit.DataImport.Identity;

/// <summary>
/// The persistence operation to perform for an imported record.
/// </summary>
public enum RecordOperation
{
    /// <summary>The record does not exist — perform an INSERT.</summary>
    Insert,

    /// <summary>The record already exists — perform an UPDATE.</summary>
    Update,
}
