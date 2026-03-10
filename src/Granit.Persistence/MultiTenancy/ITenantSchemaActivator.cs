using System.Data.Common;

namespace Granit.Persistence.MultiTenancy;

/// <summary>
/// Activates a tenant schema on a database connection immediately after it is opened.
/// </summary>
/// <remarks>
/// <para>
/// This abstraction decouples schema-based tenant isolation from any specific database provider.
/// Granit ships three built-in implementations:
/// </para>
/// <list type="bullet">
///   <item><see cref="PostgresqlTenantSchemaActivator"/> (default) — <c>SET search_path TO "schema", public</c></item>
///   <item><see cref="MySqlTenantSchemaActivator"/> — <c>USE `schema`</c> (MySQL / MariaDB)</item>
///   <item><see cref="OracleTenantSchemaActivator"/> — <c>ALTER SESSION SET CURRENT_SCHEMA = "schema"</c></item>
/// </list>
/// <para>
/// <strong>SQL Server</strong> does not support session-level schema switching. Use the
/// <c>DatabasePerTenant</c> or <c>SharedDatabase</c> isolation strategies instead.
/// </para>
/// <para>
/// The default is <see cref="PostgresqlTenantSchemaActivator"/>, registered via
/// <see cref="Extensions.PersistenceTenantExtensions"/> with <c>TryAddSingleton</c>.
/// To use a different provider, register your <see cref="ITenantSchemaActivator"/>
/// before calling <c>AddTenantPerSchemaDbContext</c>.
/// </para>
/// <para>
/// <strong>Connection pool safety (critical)</strong> — implementations MUST execute the
/// schema switch unconditionally on every call. Pooled connections retain the previous
/// tenant's schema; skipping activation would cause a cross-tenant data breach (HDS).
/// </para>
/// </remarks>
public interface ITenantSchemaActivator
{
    /// <summary>
    /// Activates the specified schema on the given connection (synchronous path).
    /// </summary>
    /// <param name="connection">The opened database connection.</param>
    /// <param name="schemaName">
    /// The validated schema name returned by <see cref="ITenantSchemaProvider"/>.
    /// </param>
    void ActivateSchema(DbConnection connection, string schemaName);

    /// <summary>
    /// Activates the specified schema on the given connection (asynchronous path).
    /// </summary>
    /// <param name="connection">The opened database connection.</param>
    /// <param name="schemaName">
    /// The validated schema name returned by <see cref="ITenantSchemaProvider"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ActivateSchemaAsync(
        DbConnection connection,
        string schemaName,
        CancellationToken cancellationToken = default);
}
