using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

namespace Granit.Wolverine.Postgresql.Extensions;

/// <summary>
/// Extension methods for registering the Granit Wolverine PostgreSQL provider.
/// </summary>
public static class WolverinePostgresqlHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds PostgreSQL-backed durable messaging (Outbox) for Wolverine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires <c>AddGranitWolverine()</c> to be called first on the builder,
    /// or use <see cref="GranitWolverinePostgresqlModule"/> which handles the ordering
    /// automatically via <see cref="Granit.Core.Modularity.DependsOnAttribute"/>.
    /// </para>
    /// <para>
    /// Configures:
    /// <list type="bullet">
    ///   <item>PostgreSQL Outbox — durable at-least-once delivery (HDS-compliant).</item>
    ///   <item>EF Core transaction integration — message dispatch atomic with DB write.</item>
    /// </list>
    /// </para>
    /// <para>
    /// EF Core <c>DbContext</c> types that participate in Wolverine transactions must be
    /// registered via <c>services.AddDbContextWithWolverineIntegration&lt;TContext&gt;()</c>
    /// instead of the standard <c>services.AddDbContext&lt;TContext&gt;()</c>.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="connectionString">PostgreSQL connection string for the Wolverine Outbox tables.</param>
    /// <param name="configure">Optional additional Wolverine configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitWolverineWithPostgresql(
        this IHostApplicationBuilder builder,
        string connectionString,
        Action<WolverineOptions>? configure = null)
    {
        builder.UseWolverine(opts =>
        {
            opts.PersistMessagesWithPostgresql(connectionString);
            opts.UseEntityFrameworkCoreTransactions();
            opts.Policies.AutoApplyTransactions();

            configure?.Invoke(opts);
        });

        return builder;
    }
}
