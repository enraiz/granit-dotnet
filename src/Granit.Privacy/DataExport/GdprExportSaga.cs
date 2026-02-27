using Granit.Privacy.DataExport.Events;
using Granit.Privacy.Options;
using Microsoft.Extensions.Options;
using Wolverine;

namespace Granit.Privacy.DataExport;

/// <summary>
/// Wolverine Stateful Saga implementing the GDPR export scatter-gather pattern (RGPD Art. 15/20).
/// </summary>
/// <remarks>
/// <para>
/// Flow:
/// <list type="number">
///   <item><see cref="PersonalDataRequestedEvent"/> starts the Saga and schedules a timeout.</item>
///   <item>Each registered data provider handles the event, uploads its data fragment to
///   BlobStorage, and publishes a <see cref="PersonalDataPreparedEvent"/> with the
///   <c>BlobReferenceId</c>.</item>
///   <item>The Saga collects fragments. When all expected fragments arrive it publishes
///   <see cref="ExportCompletedEvent"/> with <c>IsPartial = false</c>.</item>
///   <item>If the timeout fires before all fragments arrive, the Saga publishes a partial
///   <see cref="ExportCompletedEvent"/> with <c>IsPartial = true</c> listing missing providers.</item>
/// </list>
/// </para>
/// <para>
/// HDS compliance: fragments are referenced by <c>BlobReferenceId</c> only — raw personal data
/// is never stored in the Saga state or event payloads.
/// </para>
/// <para>
/// The <c>ArchiveBlobReferenceId</c> in <see cref="ExportCompletedEvent"/> uses the convention
/// <c>"gdpr-export/{RequestId}"</c>. The application assembles the final archive under this key
/// using the individual fragment references.
/// </para>
/// </remarks>
public sealed class GdprExportSaga : Saga
{
    /// <summary>Saga correlation ID — equals <see cref="PersonalDataRequestedEvent.RequestId"/>.</summary>
    public Guid Id { get; set; }

    /// <summary>User whose data is being exported.</summary>
    public Guid UserId { get; set; }

    /// <summary>Number of fragments expected (from <see cref="IDataProviderRegistry.Count"/>).</summary>
    public int ExpectedCount { get; set; }

    /// <summary>Fragments received from data providers (BlobReferenceId only — HDS).</summary>
    public List<ReceivedFragment> ReceivedFragments { get; set; } = [];

    /// <summary>Provider names that have not yet responded.</summary>
    public List<string> PendingProviders { get; set; } = [];

    /// <summary>
    /// Starts the Saga when a data subject requests export of their personal data.
    /// If no providers are registered, completes immediately.
    /// Otherwise, schedules a timeout to handle unresponsive providers.
    /// </summary>
    public async Task<ExportCompletedEvent?> StartAsync(
        PersonalDataRequestedEvent evt,
        IDataProviderRegistry registry,
        IOptions<GranitPrivacyOptions> options,
        IMessageContext context)
    {
        Id = evt.RequestId;
        UserId = evt.UserId;
        ExpectedCount = registry.Count;
        PendingProviders = [.. registry.GetAll()];

        if (ExpectedCount == 0)
        {
            MarkCompleted();
            return new ExportCompletedEvent(Id, UserId, $"gdpr-export/{Id}", IsPartial: false, []);
        }

        await context.ScheduleAsync(
            new ExportTimedOutEvent(evt.RequestId),
            TimeSpan.FromMinutes(options.Value.ExportTimeoutMinutes));

        return null;
    }

    /// <summary>
    /// Handles a fragment prepared by a data provider.
    /// Completes the Saga if all expected fragments have been received.
    /// </summary>
    public ExportCompletedEvent? Handle(PersonalDataPreparedEvent evt)
    {
        ReceivedFragments.Add(new ReceivedFragment(evt.ProviderName, evt.BlobReferenceId, evt.ContentType));
        PendingProviders.Remove(evt.ProviderName);

        if (ReceivedFragments.Count < ExpectedCount)
        {
            return null;
        }

        MarkCompleted();
        return new ExportCompletedEvent(Id, UserId, $"gdpr-export/{Id}", IsPartial: false, []);
    }

    /// <summary>
    /// Handles the timeout event.
    /// Publishes a partial <see cref="ExportCompletedEvent"/> with whatever fragments arrived.
    /// </summary>
    public ExportCompletedEvent Handle(ExportTimedOutEvent evt)
    {
        MarkCompleted();
        return new ExportCompletedEvent(
            Id,
            UserId,
            $"gdpr-export/{Id}",
            IsPartial: true,
            PendingProviders.AsReadOnly());
    }
}
