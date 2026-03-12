using Granit.BackgroundJobs.Abstractions;

namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// No-op <see cref="IDeadLetterQueueInspector"/> that always returns empty counts.
/// Used when no durable messaging infrastructure is available.
/// </summary>
internal sealed class NullDeadLetterQueueInspector : IDeadLetterQueueInspector
{
    private static readonly IReadOnlyDictionary<string, long> Empty =
        new Dictionary<string, long>();

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<string, long>> GetCountsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Empty);
}
