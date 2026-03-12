using Granit.BackgroundJobs.Abstractions;
using JasperFx.Core;
using Microsoft.Extensions.Logging;
using Wolverine.Persistence.Durability;
using Wolverine.Persistence.Durability.DeadLetterManagement;

namespace Granit.BackgroundJobs.Wolverine.Internal;

/// <summary>
/// <see cref="IDeadLetterQueueInspector"/> implementation backed by Wolverine's
/// <see cref="IMessageStore"/> for DLQ inspection.
/// </summary>
internal sealed partial class WolverineDeadLetterQueueInspector(
    ILogger<WolverineDeadLetterQueueInspector> logger,
    IMessageStore? messageStore = null) : IDeadLetterQueueInspector
{
    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, long>> GetCountsAsync(
        CancellationToken cancellationToken = default)
    {
        if (messageStore is null)
        {
            return new Dictionary<string, long>();
        }

        try
        {
            IReadOnlyList<DeadLetterQueueCount> counts =
                await messageStore.DeadLetters.SummarizeAllAsync(
                    string.Empty, TimeRange.AllTime(), cancellationToken).ConfigureAwait(false);

            return counts.ToDictionary(
                c => c.MessageType,
                c => (long)c.Count,
                StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            LogDlqQueryFailed(ex);
            return new Dictionary<string, long>();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to query Wolverine Dead Letter Queue; DeadLetterCount will be 0 for all jobs")]
    private partial void LogDlqQueryFailed(Exception exception);
}
