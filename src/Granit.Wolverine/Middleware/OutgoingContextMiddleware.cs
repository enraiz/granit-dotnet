using System.Diagnostics;
using Granit.Core.MultiTenancy;
using Granit.Security;
using Wolverine;

namespace Granit.Wolverine.Middleware;

/// <summary>
/// Wolverine middleware that propagates the current tenant, user, and trace context
/// into outgoing message envelopes via standard headers.
/// </summary>
/// <remarks>
/// <para>
/// Injects <c>X-Tenant-Id</c> from <see cref="ICurrentTenant.Id"/>,
/// <c>X-User-Id</c> from <see cref="ICurrentUserService.UserId"/>, and
/// <c>traceparent</c> from <see cref="Activity.Current"/> (W3C Trace Context)
/// into the <see cref="Envelope.Headers"/> dictionary before the message exits the pipeline.
/// Headers are omitted when the corresponding context is absent.
/// </para>
/// <para>
/// Registered globally via <c>opts.Policies.AddMiddleware&lt;OutgoingContextMiddleware&gt;()</c>
/// in <c>AddGranitWolverine()</c>. Context is consumed by
/// <see cref="Granit.Wolverine.Behaviors.TenantContextBehavior"/>,
/// <see cref="Granit.Wolverine.Behaviors.UserContextBehavior"/>, and
/// <see cref="Granit.Wolverine.Behaviors.TraceContextBehavior"/> on the receiving side.
/// </para>
/// <para>Compliance: no PII is logged — headers flow only inside message envelopes.</para>
/// </remarks>
public sealed class OutgoingContextMiddleware(
    ICurrentTenant currentTenant,
    ICurrentUserService currentUserService)
{
    internal const string TenantIdHeader = "X-Tenant-Id";
    internal const string UserIdHeader = "X-User-Id";
    internal const string UserFirstNameHeader = "X-User-FirstName";
    internal const string UserLastNameHeader = "X-User-LastName";
    internal const string TraceParentHeader = "traceparent";

    /// <summary>
    /// Injects tenant, user, and trace context headers into <paramref name="envelope"/> before dispatch.
    /// </summary>
    /// <param name="envelope">The outgoing Wolverine envelope.</param>
    public void Before(Envelope envelope)
    {
        if (currentTenant.Id.HasValue)
        {
            envelope.Headers[TenantIdHeader] = currentTenant.Id.Value.ToString();
        }

        if (currentUserService.IsAuthenticated && currentUserService.UserId is { Length: > 0 } userId)
        {
            envelope.Headers[UserIdHeader] = userId;

            if (currentUserService.FirstName is { Length: > 0 } firstName)
            {
                envelope.Headers[UserFirstNameHeader] = firstName;
            }

            if (currentUserService.LastName is { Length: > 0 } lastName)
            {
                envelope.Headers[UserLastNameHeader] = lastName;
            }
        }

        string? traceParent = Activity.Current?.Id;
        if (traceParent is not null)
        {
            envelope.Headers[TraceParentHeader] = traceParent;
        }
    }
}
