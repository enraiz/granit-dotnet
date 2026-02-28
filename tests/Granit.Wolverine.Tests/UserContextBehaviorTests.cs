// =============================================================================
// Tests - UserContextBehavior
// =============================================================================
// Verifies that X-User-Id header is correctly forwarded to IWolverineUserContextSetter
// in incoming Wolverine envelopes, and that the scope is disposed on After().
// =============================================================================

using Granit.Wolverine.Behaviors;
using Granit.Wolverine.Internal;
using Granit.Wolverine.Middleware;
using NSubstitute;
using Shouldly;
using Wolverine;
using Xunit;

namespace Granit.Wolverine.Tests;

public sealed class UserContextBehaviorTests
{
    [Fact]
    public void Before_WithUserHeader_CallsSetterChange()
    {
        const string userId = "user-abc";
        IWolverineUserContextSetter setter = Substitute.For<IWolverineUserContextSetter>();
        setter.Change(userId).Returns(Substitute.For<IDisposable>());

        UserContextBehavior behavior = new(setter);
        Envelope envelope = new();
        envelope.Headers[OutgoingContextMiddleware.UserIdHeader] = userId;

        behavior.Before(envelope);

        setter.Received(1).Change(userId);
    }

    [Fact]
    public void Before_WithMissingHeader_DoesNotCallSetter()
    {
        IWolverineUserContextSetter setter = Substitute.For<IWolverineUserContextSetter>();

        UserContextBehavior behavior = new(setter);
        Envelope envelope = new();

        behavior.Before(envelope);

        setter.DidNotReceive().Change(Arg.Any<string?>());
    }

    [Fact]
    public void Before_WithEmptyHeader_DoesNotCallSetter()
    {
        IWolverineUserContextSetter setter = Substitute.For<IWolverineUserContextSetter>();

        UserContextBehavior behavior = new(setter);
        Envelope envelope = new();
        envelope.Headers[OutgoingContextMiddleware.UserIdHeader] = string.Empty;

        behavior.Before(envelope);

        setter.DidNotReceive().Change(Arg.Any<string?>());
    }

    [Fact]
    public void After_WhenScopeWasSet_DisposesScope()
    {
        const string userId = "user-xyz";
        IDisposable scope = Substitute.For<IDisposable>();

        IWolverineUserContextSetter setter = Substitute.For<IWolverineUserContextSetter>();
        setter.Change(userId).Returns(scope);

        UserContextBehavior behavior = new(setter);
        Envelope envelope = new();
        envelope.Headers[OutgoingContextMiddleware.UserIdHeader] = userId;

        behavior.Before(envelope);
        behavior.After();

        scope.Received(1).Dispose();
    }

    [Fact]
    public void After_WhenNoScopeWasSet_DoesNotThrow()
    {
        IWolverineUserContextSetter setter = Substitute.For<IWolverineUserContextSetter>();
        UserContextBehavior behavior = new(setter);

        Action act = behavior.After;

        Should.NotThrow(act);
    }
}
