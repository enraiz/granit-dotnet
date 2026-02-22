namespace DigitalDynamics.Foundation.Core.Domain;

/// <summary>
/// Audit trail entry for HDS compliance.
/// Records who did what, when, and on which entity.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>Unique identifier of the audit entry.</summary>
    public Guid Id { get; init; }

    /// <summary>Operation timestamp (UTC).</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Identifier of the user who performed the operation.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Operation type: Create, Update, Delete, SoftDelete.</summary>
    public string Operation { get; init; } = string.Empty;

    /// <summary>CLR type of the affected entity.</summary>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>Identifier of the affected entity.</summary>
    public string EntityId { get; init; } = string.Empty;

    /// <summary>Modified properties, serialized as JSON (without sensitive data).</summary>
    public string? Changes { get; init; }

    /// <summary>Source IP address of the request.</summary>
    public string? IpAddress { get; init; }

    /// <summary>Client User-Agent.</summary>
    public string? UserAgent { get; init; }
}
