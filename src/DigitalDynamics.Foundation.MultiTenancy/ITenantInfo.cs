// =============================================================================
// ITenantInfo - Informations sur un tenant
// =============================================================================

namespace DigitalDynamics.Foundation.MultiTenancy;

/// <summary>
/// Informations sur un tenant.
/// </summary>
public interface ITenantInfo
{
    /// <summary>Identifiant unique du tenant.</summary>
    Guid? Id { get; }

    /// <summary>Nom du tenant.</summary>
    string? Name { get; }
}
