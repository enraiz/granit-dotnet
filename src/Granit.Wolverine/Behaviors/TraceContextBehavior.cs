using System.Diagnostics;
using Granit.Wolverine.Diagnostics;
using Granit.Wolverine.Middleware;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Granit.Wolverine.Behaviors;

/// <summary>
/// Wolverine middleware that restores the W3C Trace Context from incoming message
/// envelope headers, linking asynchronous Outbox processing to the originating
/// HTTP request trace in Tempo/Grafana.
/// </summary>
/// <remarks>
/// <para>
/// Reads the <c>traceparent</c> header (W3C Trace Context, RFC) injected by
/// <see cref="OutgoingContextMiddleware"/> on the publisher side and starts a bridge
/// <see cref="Activity"/> whose parent is the original HTTP request span.
/// All spans created during handler execution (EF Core queries, HttpClient calls, etc.)
/// become children of this bridge activity and are therefore visible under the original
/// <c>trace-id</c> in Grafana/Tempo.
/// </para>
/// <para>
/// If the header is absent, the behavior is no-op: the handler runs without trace context.
/// If the header is malformed (non-W3C format), a warning is logged and the handler runs normally.
/// </para>
/// <para>
/// The bridge activity is recorded by the <c>Granit.Wolverine</c>
/// <see cref="ActivitySource"/>, which must be registered via
/// <c>AddSource(WolverineActivitySource.Name)</c> in the OpenTelemetry tracer provider.
/// <c>Granit.Observability</c> registers it automatically.
/// </para>
/// </remarks>
public sealed class TraceContextBehavior(ILogger<TraceContextBehavior> logger)
{
    private Activity? _activity;

    /// <summary>
    /// Restores the trace context from the <c>traceparent</c> header before the handler runs.
    /// </summary>
    /// <param name="envelope">The incoming Wolverine envelope.</param>
    public void Before(Envelope envelope)
    {
        if (!envelope.Headers.TryGetValue(OutgoingContextMiddleware.TraceParentHeader, out string? traceParent)
            || string.IsNullOrEmpty(traceParent))
        {
            return;
        }

        if (!ActivityContext.TryParse(traceParent, traceState: null, isRemote: true, out ActivityContext parentContext))
        {
            logger.LogWarning(
                "Wolverine envelope {EnvelopeId} contains a malformed traceparent header: {TraceParent}. Distributed trace context will not be restored.",
                envelope.Id,
                traceParent);
            return;
        }

        _activity = WolverineActivitySource.Source.StartActivity(
            "wolverine.message.handle",
            ActivityKind.Consumer,
            parentContext);

        if (_activity is not null)
        {
            _activity.SetTag("messaging.system", "wolverine");
            _activity.SetTag("messaging.operation", "process");
            _activity.SetTag("messaging.message_id", envelope.Id.ToString());

            if (envelope.MessageType is { Length: > 0 } messageType)
            {
                _activity.SetTag("messaging.message_type", messageType);
            }
        }
    }

    /// <summary>Stops the bridge activity after the handler completes.</summary>
    public void After() => _activity?.Dispose();
}
