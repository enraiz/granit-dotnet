namespace Granit.Privacy.DataExport;

/// <summary>
/// State for the GDPR export Saga (scatter-gather pattern).
/// Tracks received fragments and determines when the export is complete.
/// </summary>
public sealed class GdprExportSagaState
{
    /// <summary>Unique request identifier.</summary>
    public Guid RequestId { get; set; }

    /// <summary>User whose data is being exported.</summary>
    public Guid UserId { get; set; }

    /// <summary>Number of fragments expected (from <see cref="IDataProviderRegistry.Count"/>).</summary>
    public int ExpectedCount { get; set; }

    /// <summary>BlobReferenceIds of received fragments.</summary>
    public List<ReceivedFragment> ReceivedFragments { get; set; } = [];

    /// <summary>Provider names that have not yet responded.</summary>
    public List<string> AllProviders { get; set; } = [];

    /// <summary>Whether the export has been completed (all fragments or timeout).</summary>
    public bool IsCompleted { get; set; }
}

/// <summary>
/// A fragment received from a data provider during the GDPR export Saga.
/// </summary>
public sealed record ReceivedFragment(string ProviderName, string BlobReferenceId, string ContentType);
