using Granit.Core.Events;

namespace Granit.Privacy.DataExport.Events;

/// <summary>
/// Published by each data provider after preparing its fragment for a data export request.
/// The <see cref="BlobReferenceId"/> points to the fragment stored in BlobStorage —
/// raw data is never included in the event payload (HDS compliance).
/// </summary>
public sealed record PersonalDataPreparedEvent(
    Guid RequestId,
    string ProviderName,
    string BlobReferenceId,
    string ContentType) : IIntegrationEvent;
