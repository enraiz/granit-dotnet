namespace Granit.AI;

/// <summary>
/// Records AI usage metrics (token counts, cost estimates) per interaction.
/// </summary>
/// <remarks>
/// Called by the usage tracking middleware in the <c>IChatClient</c> pipeline.
/// Default implementation: <see cref="Internal.NullAIUsageTracker"/> (discards records).
/// Override with <c>Granit.AI.EntityFrameworkCore</c> for persistent storage.
/// </remarks>
public interface IAIUsageTracker
{
    /// <summary>
    /// Records usage from a completed AI interaction.
    /// </summary>
    /// <param name="record">Usage record to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordAsync(AIUsageRecord record, CancellationToken cancellationToken = default);
}
