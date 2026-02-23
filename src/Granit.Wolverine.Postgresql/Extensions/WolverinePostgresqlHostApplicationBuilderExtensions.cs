using Granit.Wolverine.Postgresql.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
    /// Reads <see cref="WolverinePostgresqlOptions"/> from the
    /// <c>"WolverinePostgresql"</c> configuration section and validates at startup.
    /// </para>
    /// <para>
    /// Configures:
    /// <list type="bullet">
    ///   <item>PostgreSQL Outbox — durable at-least-once delivery (HDS-compliant).</item>
    ///   <item>EF Core transaction integration — message dispatch atomic with DB write.</item>
    ///   <item><see cref="WolverinePostgresqlOptions.TransactionMode"/> applied to all handlers.</item>
    /// </list>
    /// </para>
    /// <para>
    /// EF Core <c>DbContext</c> types that participate in Wolverine transactions must be
    /// registered via <c>services.AddDbContextWithWolverineIntegration&lt;TContext&gt;()</c>
    /// instead of the standard <c>services.AddDbContext&lt;TContext&gt;()</c>.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Optional additional Wolverine configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitWolverineWithPostgresql(
        this IHostApplicationBuilder builder,
        Action<WolverineOptions>? configure = null)
    {
        // Bind and validate options at startup via DI.
        builder.Services
            .AddOptions<WolverinePostgresqlOptions>()
            .BindConfiguration(WolverinePostgresqlOptions.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<WolverinePostgresqlOptions>,
            WolverinePostgresqlOptionsValidator>();

        // Read options directly from IConfiguration: the DI container is not yet
        // built at this point, so IOptions<> is not resolvable inside UseWolverine().
        WolverinePostgresqlOptions options = new();
        builder.Configuration
            .GetSection(WolverinePostgresqlOptions.SectionName)
            .Bind(options);

        builder.UseWolverine(opts =>
        {
            opts.PersistMessagesWithPostgresql(options.TransportConnectionString);
            opts.UseEntityFrameworkCoreTransactions(options.TransactionMode);
            opts.Policies.AutoApplyTransactions();

            configure?.Invoke(opts);
        });

        return builder;
    }
}
