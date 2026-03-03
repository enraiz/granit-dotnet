using Granit.Core.Domain;

namespace Granit.DataExchange.Export;

/// <summary>
/// Represents an export job with its lifecycle state and metadata.
/// </summary>
/// <remarks>
/// Inherits <see cref="AuditedEntity"/> for HDS-compliant audit trail
/// (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy).
/// </remarks>
public sealed class ExportJob : AuditedEntity
{
    /// <summary>
    /// The export definition name (e.g. <c>"Guava.PatientExport"</c>).
    /// Links to the registered <c>ExportDefinition&lt;T, TFilter&gt;</c>.
    /// </summary>
    public required string DefinitionName { get; set; }

    /// <summary>
    /// Output format (<c>"xlsx"</c> or <c>"csv"</c>).
    /// </summary>
    public required string Format { get; set; }

    /// <summary>
    /// Serialized <see cref="ExportRequest"/> (JSON). Preserved for auditability and retry.
    /// </summary>
    public required string RequestJson { get; set; }

    /// <summary>
    /// Current lifecycle status.
    /// </summary>
    public ExportJobStatus Status { get; set; } = ExportJobStatus.Queued;

    /// <summary>
    /// Reference to the generated file in blob storage. Set when <see cref="Status"/> is
    /// <see cref="ExportJobStatus.Completed"/>.
    /// </summary>
    public string? BlobReference { get; set; }

    /// <summary>
    /// Generated file name for download (e.g. <c>"patients_2026-03-03.xlsx"</c>).
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Number of rows exported.
    /// </summary>
    public int? RowCount { get; set; }

    /// <summary>
    /// Error message if <see cref="Status"/> is <see cref="ExportJobStatus.Failed"/>.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when the export completed (success or failure).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Tenant identifier. Soft dependency on <c>ICurrentTenant</c>.
    /// </summary>
    public Guid? TenantId { get; set; }
}
