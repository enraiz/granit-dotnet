using Granit.DataExchange.Export.Messages;
using Granit.DataExchange.Import.Messages;

namespace Granit.DataExchange.Internal;

/// <summary>
/// No-op default implementation of <see cref="IDataExchangeEventPublisher"/>.
/// </summary>
/// <remarks>
/// Replaced by the Wolverine-backed implementation when <c>GranitDataExchangeWolverineModule</c>
/// is loaded.
/// </remarks>
internal sealed class NullDataExchangeEventPublisher : IDataExchangeEventPublisher
{
    /// <inheritdoc/>
    public Task PublishAsync(ImportJobCompletedEvent evt, CancellationToken ct = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task PublishAsync(ExportJobCompletedEvent evt, CancellationToken ct = default) =>
        Task.CompletedTask;
}
