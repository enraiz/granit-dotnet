using Granit.Core.MultiTenancy;
using Granit.Security;
using Wolverine;

namespace Granit.Wolverine.Middleware;

/// <summary>
/// Wolverine middleware that propagates the current tenant and user context
/// into outgoing message envelopes via standard headers.
/// </summary>
/// <remarks>
/// <para>
/// Injects <c>X-Tenant-Id</c> from <see cref="ICurrentTenant.Id"/> and
/// <c>X-User-Id</c> from <see cref="ICurrentUserService.UserId"/> into the
/// <see cref="Envelope.Headers"/> dictionary before the message exits the pipeline.
/// Headers are omitted when no tenant or user is active (no empty values written).
/// </para>
/// <para>
/// Registered globally via <c>opts.Policies.AddMiddleware&lt;OutgoingContextMiddleware&gt;()</c>
/// in <c>AddGranitWolverine()</c>. Context is consumed by
/// <see cref="Granit.Wolverine.Behaviors.TenantContextBehavior"/> and
/// <see cref="Granit.Wolverine.Behaviors.UserContextBehavior"/> on the receiving side.
/// </para>
/// <para>Compliance: no PII is logged — headers flow only inside message envelopes.</para>
/// </remarks>
public sealed class OutgoingContextMiddleware(
    ICurrentTenant currentTenant,
    ICurrentUserService currentUserService)
{
    internal const string TenantIdHeader = "X-Tenant-Id";
    internal const string UserIdHeader = "X-User-Id";

    /// <summary>
    /// Injects tenant and user headers into <paramref name="envelope"/> before dispatch.
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
        }
    }
}
