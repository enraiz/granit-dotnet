namespace Granit.BackgroundJobs.Abstractions;

/// <summary>
/// Inspects the Dead Letter Queue for failed background job messages.
/// </summary>
/// <remarks>
/// The default implementation returns empty counts (graceful degradation
/// when no durable messaging is available). When <c>Granit.BackgroundJobs.Wolverine</c>
/// is loaded, this is replaced with a Wolverine <c>IMessageStore</c>-backed implementation.
/// </remarks>
public interface IDeadLetterQueueInspector
{
    /// <summary>
    /// Returns DLQ counts grouped by message type name.
    /// Returns an empty dictionary when the DLQ is not available.
    /// </summary>
    Task<IReadOnlyDictionary<string, long>> GetCountsAsync(CancellationToken cancellationToken = default);
}
