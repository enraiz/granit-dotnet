using System.ComponentModel.DataAnnotations;
using Wolverine.Persistence;

namespace Granit.Wolverine.Postgresql.Options;

/// <summary>
/// Configuration options for the PostgreSQL Wolverine provider.
/// Bound from the <c>"WolverinePostgresql"</c> section of <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>TransportConnectionString</c> must point to the ISO 27001 PostgreSQL instance on European sovereign infrastructure.
/// </para>
/// <para>
/// <c>TransactionMode</c> defaults to <see cref="TransactionMiddlewareMode.Eager"/>
/// (explicit <c>BeginTransactionAsync</c>) to satisfy ISO 27001 audit requirements.
/// Use <see cref="TransactionMiddlewareMode.Lightweight"/> only for non-critical background
/// processes where <c>SaveChangesAsync</c>-level isolation is sufficient.
/// </para>
/// </remarks>
public sealed class WolverinePostgresqlOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "WolverinePostgresql";

    /// <summary>
    /// PostgreSQL connection string for the Wolverine Outbox tables.
    /// Must be non-empty. Required for ISO 27001-compliant durable messaging.
    /// </summary>
    [Required]
    public string TransportConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// EF Core transaction wrapping mode for Wolverine handlers.
    /// Default: <see cref="TransactionMiddlewareMode.Eager"/> (ISO 27001-recommended).
    /// </summary>
    public TransactionMiddlewareMode TransactionMode { get; set; } = TransactionMiddlewareMode.Eager;
}
