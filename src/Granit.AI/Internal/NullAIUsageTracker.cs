namespace Granit.AI.Internal;

/// <summary>
/// No-op usage tracker used when no persistence adapter is registered.
/// </summary>
internal sealed class NullAIUsageTracker : IAIUsageTracker
{
    public Task RecordAsync(AIUsageRecord record, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
