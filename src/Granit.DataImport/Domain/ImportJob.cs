using Granit.Core.Domain;

namespace Granit.DataImport.Domain;

/// <summary>
/// Represents an import job with its lifecycle state and metadata.
/// </summary>
/// <remarks>
/// Inherits <see cref="AuditedEntity"/> for HDS-compliant audit trail
/// (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy).
/// </remarks>
public sealed class ImportJob : AuditedEntity
{
    /// <summary>
    /// The import definition name (e.g. <c>"Guava.PatientImport"</c>).
    /// Links to the registered <c>ImportDefinition&lt;T&gt;</c>.
    /// </summary>
    public required string DefinitionName { get; set; }

    /// <summary>
    /// CLR type name of the target entity (e.g. <c>"Patient"</c>).
    /// </summary>
    public required string EntityTypeName { get; set; }

    /// <summary>
    /// Original file name as uploaded by the user.
    /// </summary>
    public required string OriginalFileName { get; set; }

    /// <summary>
    /// MIME type of the uploaded file (e.g. <c>"text/csv"</c>).
    /// </summary>
    public required string MimeType { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public required long FileSizeBytes { get; set; }

    /// <summary>
    /// Reference to the file in blob storage.
    /// </summary>
    public required string BlobReference { get; set; }

    /// <summary>
    /// Current lifecycle status.
    /// </summary>
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Created;

    /// <summary>
    /// Serialized column mappings (JSON). Set after user confirmation.
    /// </summary>
    public string? MappingsJson { get; set; }

    /// <summary>
    /// Serialized import report (JSON). Set after execution completes.
    /// </summary>
    public string? ReportJson { get; set; }

    /// <summary>
    /// Timestamp when the import completed (success, partial, or failure).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Tenant identifier. Soft dependency on <c>ICurrentTenant</c>.
    /// </summary>
    public Guid? TenantId { get; set; }
}
