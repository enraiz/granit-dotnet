using Granit.Domain;

namespace Granit.Identity.Local.Domain;

/// <summary>
/// Join entity linking a canonical
/// <c>Granit.Identity.Domain.User</c> to a
/// <see cref="GranitUserGroup"/>.
/// </summary>
/// <remarks>
/// Per ADR-051 B-step 4, <see cref="UserId"/> references the canonical
/// <c>Granit.Identity.Domain.User.Id</c>, not
/// <c>LocalIdentity.Id</c> directly. Both Ids share the same Guid
/// value (alignment guarantee from B-step 2 / B-step 3) so historical
/// references continue to resolve, but the *meaning* is now "any user
/// — local or federated" rather than "a local-side credential record".
/// Group membership therefore applies to federated users too once they
/// are hydrated through <c>CachedUserLookupService</c>.
/// </remarks>
public class GranitUserGroupMember : AuditedEntity, IMultiTenant
{
    /// <summary>Gets or sets the group identifier.</summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Gets or sets the canonical user identifier — references
    /// <c>Granit.Identity.Domain.User.Id</c>.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the tenant identifier for multi-tenant isolation.</summary>
    public Guid? TenantId { get; set; }
}
