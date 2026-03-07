using Granit.DataExchange.Export.Messages;
using Granit.DataExchange.Import.Messages;

namespace Granit.DataExchange;

/// <summary>
/// Publishes data exchange lifecycle events for consumption by notification handlers,
/// audit loggers, and other downstream consumers.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation is a no-op. Applications using Wolverine should register
/// <c>Granit.DataExchange.Wolverine</c> which replaces it with a durable Outbox-backed
/// implementation via <c>IMessageBus.PublishAsync()</c>.
/// </para>
/// </remarks>
public interface IDataExchangeEventPublisher
{
    /// <summary>
    /// Publishes an import job completion event.
    /// </summary>
    Task PublishAsync(ImportJobCompletedEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Publishes an export job completion event.
    /// </summary>
    Task PublishAsync(ExportJobCompletedEvent evt, CancellationToken ct = default);
}
