using System.Data.Common;
using Granit.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Granit.Persistence.MultiTenancy;

/// <summary>
/// EF Core connection interceptor that executes <c>SET search_path TO {schema}, public</c>
/// immediately after each physical connection is opened.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Connection pool safety (critical)</strong> — Npgsql does not close physical
/// connections after use: it returns them to the connection pool. A connection that
/// previously served tenant A retains <c>search_path = tenant_a</c> in the pool.
/// If tenant B acquires that connection and this interceptor did not run, tenant B
/// would read tenant A's data — a catastrophic HDS data breach.
/// </para>
/// <para>
/// To prevent this, both <see cref="ConnectionOpened"/> and <see cref="ConnectionOpenedAsync"/>
/// execute <c>SET search_path</c> <em>unconditionally</em> on every lease from the pool.
/// There is no cache, no "already set" check, and no escape condition.
/// </para>
/// <para>
/// When <see cref="ICurrentTenant.IsAvailable"/> is <c>false</c> (no active tenant),
/// <c>search_path</c> is left unchanged — the connection uses the PostgreSQL default
/// (<c>public</c>). This is intentional for host-level maintenance connections that
/// operate outside any tenant context.
/// </para>
/// </remarks>
internal sealed class TenantSchemaConnectionInterceptor(
    ICurrentTenant currentTenant,
    ITenantSchemaProvider schemaProvider) : DbConnectionInterceptor
{
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ITenantSchemaProvider _schemaProvider = schemaProvider;

    /// <inheritdoc/>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        if (!_currentTenant.IsAvailable)
        {
            return;
        }

        // Synchronous path: convert to Task before blocking to satisfy CA2012.
        // DefaultTenantSchemaProvider returns a completed ValueTask so AsTask() is allocation-free.
        // Custom implementations should avoid async I/O in GetSchemaNameAsync when called
        // from the synchronous EF Core path.
        string schema = _schemaProvider
            .GetSchemaNameAsync(_currentTenant.Id!.Value)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        SetSearchPath(connection, schema);
    }

    /// <inheritdoc/>
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (!_currentTenant.IsAvailable)
        {
            return;
        }

        string schema = await _schemaProvider
            .GetSchemaNameAsync(_currentTenant.Id!.Value, cancellationToken)
            .ConfigureAwait(false);

        await SetSearchPathAsync(connection, schema, cancellationToken)
            .ConfigureAwait(false);
    }

    private static void SetSearchPath(DbConnection connection, string schema)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = $"SET search_path TO {schema}, public";
        cmd.ExecuteNonQuery();
    }

    private static async Task SetSearchPathAsync(
        DbConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = $"SET search_path TO {schema}, public";
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
