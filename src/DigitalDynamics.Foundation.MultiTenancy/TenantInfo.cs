// =============================================================================
// TenantInfo - Données d'un tenant résolu
// =============================================================================

namespace DigitalDynamics.Foundation.MultiTenancy;

/// <summary>
/// Données immuables d'un tenant résolu par un <see cref="Resolvers.ITenantResolver"/>.
/// </summary>
public sealed record TenantInfo(Guid? Id, string? Name = null) : ITenantInfo;
