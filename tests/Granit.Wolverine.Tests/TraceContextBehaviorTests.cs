// =============================================================================
// Tests - TraceContextBehavior
// =============================================================================
// Verifies that the W3C traceparent header from incoming Wolverine envelopes
// is correctly parsed and used to start a bridge Activity, and that edge cases
// (absent header, malformed value) are handled gracefully.
// =============================================================================

using System.Diagnostics;
using Granit.Wolverine.Behaviors;
using Granit.Wolverine.Middleware;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Wolverine;
using Xunit;

namespace Granit.Wolverine.Tests;

public sealed class TraceContextBehaviorTests : IDisposable
{
    // Valid W3C traceparent: version(00)-traceId(32 hex)-parentId(16 hex)-flags(01)
    private const string ValidTraceParent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

    private readonly ILogger<TraceContextBehavior> _logger = Substitute.For<ILogger<TraceContextBehavior>>();
    private readonly List<Activity> _capturedActivities = [];
    private readonly ActivityListener _listener;

    public TraceContextBehaviorTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Granit.Wolverine",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    // -------------------------------------------------------------------------
    // Scenario 1 — Valid traceparent: bridge activity is started
    // -------------------------------------------------------------------------

    [Fact]
    public void Before_WithValidTraceParent_StartsActivity()
    {
        TraceContextBehavior behavior = new(_logger);
        Envelope envelope = new();
        envelope.Headers[OutgoingContextMiddleware.TraceParentHeader] = ValidTraceParent;

        behavior.Before(envelope);

        _capturedActivities.Count.ShouldBe(1);
        _capturedActivities[0].OperationName.ShouldBe("wolverine.message.handle");
        _capturedActivities[0].Kind.ShouldBe(ActivityKind.Consumer);

        behavior.After();
    }

    [Fact]
    public void Before_WithValidTraceParent_ActivityInheritsOriginalTraceId()
    {
        TraceContextBehavior behavior = new(_logger);
        Envelope envelope = new();
        envelope.Headers[OutgoingContextMiddleware.TraceParentHeader] = ValidTraceParent;

        behavior.Before(envelope);

        var expectedTraceId = ActivityTraceId.CreateFromString("4bf92f3577b34da6a3ce929d0e0e4736");
        _capturedActivities[0].TraceId.ShouldBe(expectedTraceId);

        behavior.After();
    }

    [Fact]
    public void Before_WithValidTraceParent_SetsMessagingTags()
    {
        var messageId = Guid.NewGuid();
        TraceContextBehavior behavior = new(_logger);
        Envelope envelope = new() { Id = messageId, MessageType = "MyApp.OrderPlaced" };
        envelope.Headers[OutgoingContextMiddleware.TraceParentHeader] = ValidTraceParent;

        behavior.Before(envelope);

        Activity started = _capturedActivities[0];
        started.GetTagItem("messaging.system").ShouldBe("wolverine");
        started.GetTagItem("messaging.operation").ShouldBe("process");
        started.GetTagItem("messaging.message_id").ShouldBe(messageId.ToString());
        started.GetTagItem("messaging.message_type").ShouldBe("MyApp.OrderPlaced");

        behavior.After();
    }

    // -------------------------------------------------------------------------
    // Scenario 2 — No traceparent header: no-op
    // -------------------------------------------------------------------------

    [Fact]
    public void Before_WithMissingHeader_DoesNotStartActivity()
    {
        TraceContextBehavior behavior = new(_logger);
        Envelope envelope = new();

        behavior.Before(envelope);

        _capturedActivities.ShouldBeEmpty();
    }

    [Fact]
    public void Before_WithEmptyHeader_DoesNotStartActivity()
    {
        TraceContextBehavior behavior = new(_logger);
        Envelope envelope = new();
        envelope.Headers[OutgoingContextMiddleware.TraceParentHeader] = string.Empty;

        behavior.Before(envelope);

        _capturedActivities.ShouldBeEmpty();
    }

    // -------------------------------------------------------------------------
    // Scenario 3 — Malformed traceparent: ignored with warning log
    // -------------------------------------------------------------------------

    [Fact]
    public void Before_WithMalformedTraceParent_DoesNotThrow()
    {
        TraceContextBehavior behavior = new(_logger);
        Envelope envelope = new();
        envelope.Headers[OutgoingContextMiddleware.TraceParentHeader] = "not-a-valid-traceparent";

        Action act = () => behavior.Before(envelope);

        Should.NotThrow(act);
    }

    [Fact]
    public void Before_WithMalformedTraceParent_DoesNotStartActivity()
    {
        TraceContextBehavior behavior = new(_logger);
        Envelope envelope = new();
        envelope.Headers[OutgoingContextMiddleware.TraceParentHeader] = "not-a-valid-traceparent";

        behavior.Before(envelope);

        _capturedActivities.ShouldBeEmpty();
    }

    [Fact]
    public void Before_WithMalformedTraceParent_LogsWarning()
    {
        TraceContextBehavior behavior = new(_logger);
        Envelope envelope = new();
        envelope.Headers[OutgoingContextMiddleware.TraceParentHeader] = "not-a-valid-traceparent";

        behavior.Before(envelope);

        _logger.ReceivedWithAnyArgs(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // -------------------------------------------------------------------------
    // Scenario 4 — After: activity is disposed
    // -------------------------------------------------------------------------

    [Fact]
    public void After_WhenActivityStarted_ActivityIsStopped()
    {
        TraceContextBehavior behavior = new(_logger);
        Envelope envelope = new();
        envelope.Headers[OutgoingContextMiddleware.TraceParentHeader] = ValidTraceParent;

        behavior.Before(envelope);
        Activity started = _capturedActivities[0];
        started.Status.ShouldNotBe(ActivityStatusCode.Error);

        behavior.After();

        started.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void After_WhenNoActivityStarted_DoesNotThrow()
    {
        TraceContextBehavior behavior = new(_logger);

        Action act = behavior.After;

        Should.NotThrow(act);
    }
}
