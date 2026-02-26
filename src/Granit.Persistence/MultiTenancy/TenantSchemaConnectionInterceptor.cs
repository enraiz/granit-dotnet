using System.Data.Common;
using System.Text.RegularExpressions;
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
internal sealed partial class TenantSchemaConnectionInterceptor(
    ICurrentTenant currentTenant,
    ITenantSchemaProvider schemaProvider) : DbConnectionInterceptor
{
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ITenantSchemaProvider _schemaProvider = schemaProvider;

    /// <summary>
    /// Matches valid PostgreSQL unquoted identifiers: lower-case letters, digits, underscores,
    /// starting with a letter or underscore, 1–63 characters (NAMEDATALEN - 1).
    /// </summary>
    [GeneratedRegex(@"^[a-z_][a-z0-9_]{0,62}$")]
    private static partial Regex SafeSchemaNameRegex();

    /// <summary>
    /// Validates that <paramref name="schema"/> is a safe PostgreSQL identifier before
    /// it is embedded verbatim in a <c>SET search_path</c> command.
    /// </summary>
    /// <remarks>
    /// <c>SET search_path</c> is a session-variable command and does not accept bound
    /// parameters, so identifier injection must be prevented by strict allowlist validation
    /// rather than parameterization.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the schema name contains characters outside the allowed set, preventing
    /// the malformed command from reaching PostgreSQL.
    /// </exception>
    private static string ValidateSchemaName(string schema)
    {
        if (!SafeSchemaNameRegex().IsMatch(schema))
        {
            throw new InvalidOperationException(
                $"Schema name '{schema}' rejected: not a valid PostgreSQL identifier. " +
                "Only lower-case letters, digits, and underscores are allowed (1\u201363 characters).");
        }

        return schema;
    }

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

    /// <summary>
    /// Builds the <c>SET search_path</c> command text with a validated and double-quoted
    /// PostgreSQL identifier.
    /// </summary>
    /// <remarks>
    /// <c>SET search_path</c> is a session-variable command and does not accept bound
    /// parameters. Safety is ensured by two layers:
    /// <list type="number">
    ///   <item><see cref="ValidateSchemaName"/>: strict regex allowlist (lower-case, digits, underscores).</item>
    ///   <item>Double-quoting: the identifier is wrapped in <c>"…"</c> per SQL standard,
    ///         making it a delimited identifier even if the regex were ever relaxed.</item>
    /// </list>
    /// </remarks>
    private static string BuildSetSearchPathCommand(string schema) =>
        string.Concat("SET search_path TO \"", ValidateSchemaName(schema), "\", public");

    private static void SetSearchPath(DbConnection connection, string schema)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = BuildSetSearchPathCommand(schema);
        cmd.ExecuteNonQuery();
    }

    private static async Task SetSearchPathAsync(
        DbConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = BuildSetSearchPathCommand(schema);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
