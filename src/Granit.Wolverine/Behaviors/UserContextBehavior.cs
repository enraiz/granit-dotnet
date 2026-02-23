using Granit.Wolverine.Internal;
using Granit.Wolverine.Middleware;
using Wolverine;

namespace Granit.Wolverine.Behaviors;

/// <summary>
/// Wolverine middleware that restores the current user context from incoming
/// message envelope headers in background handler threads.
/// </summary>
/// <remarks>
/// <para>
/// Reads the <c>X-User-Id</c> header set by
/// <see cref="OutgoingContextMiddleware"/> on the publisher side and activates
/// the corresponding user ID for the duration of the handler invocation via
/// <see cref="IWolverineUserContextSetter.Change"/>.
/// </para>
/// <para>
/// Without this behavior, <c>ICurrentUserService.UserId</c> returns null in background
/// handlers (no <c>HttpContext</c>), causing the EF Core audit interceptor to record
/// <c>ModifiedBy = null</c> in the HDS audit trail.
/// </para>
/// <para>
/// If the header is absent, no override is applied and the handler executes with
/// the default <c>ICurrentUserService</c> (returns null for background threads).
/// </para>
/// </remarks>
public sealed class UserContextBehavior(IWolverineUserContextSetter setter)
{
    private IDisposable? _scope;

    /// <summary>
    /// Activates the user from the <c>X-User-Id</c> header before the handler runs.
    /// </summary>
    /// <param name="envelope">The incoming Wolverine envelope.</param>
    public void Before(Envelope envelope)
    {
        if (envelope.Headers.TryGetValue(OutgoingContextMiddleware.UserIdHeader, out string? userId)
            && !string.IsNullOrEmpty(userId))
        {
            _scope = setter.Change(userId);
        }
    }

    /// <summary>Restores the previous user context after the handler completes.</summary>
    public void After() => _scope?.Dispose();
}
