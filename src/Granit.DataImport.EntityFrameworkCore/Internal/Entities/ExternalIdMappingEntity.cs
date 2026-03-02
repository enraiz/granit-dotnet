namespace Granit.DataImport.EntityFrameworkCore.Internal.Entities;

/// <summary>
/// Maps an external identifier (e.g. Odoo <c>__export__</c> ID) to an internal entity ID.
/// Used by the <c>ExternalIdResolver</c> for roundtrip INSERT/UPDATE resolution.
/// </summary>
internal sealed class ExternalIdMappingEntity
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>The import definition name.</summary>
    public required string DefinitionName { get; set; }

    /// <summary>The external identifier from the source system.</summary>
    public required string ExternalId { get; set; }

    /// <summary>The internal entity identifier in the application database.</summary>
    public Guid InternalId { get; set; }

    /// <summary>Tenant identifier. <c>null</c> when multi-tenancy is not active.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>When the mapping was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
