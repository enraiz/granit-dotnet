using Granit.Persistence.Migrations.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Granit.Persistence.Migrations.Extensions;

/// <summary>
/// Extension methods for registering the Granit zero-downtime migrations infrastructure.
/// </summary>
public static class PersistenceMigrationsHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Granit zero-downtime migrations infrastructure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers:
    /// <list type="bullet">
    ///   <item>
    ///     <see cref="MigrationProgressDbContext"/> — system <c>DbContext</c> for progress tracking.
    ///     Uses its own connection, never affected by tenant schema switches.
    ///     Committed independently from the tenant data transaction (best-effort progress).
    ///   </item>
    ///   <item>
    ///     <see cref="IMigrationCycleRegistry"/> as a thread-safe singleton.
    ///     Populate it at startup via <see cref="MigrationCycleRegistryExtensions.Register{TContext}"/>.
    ///   </item>
    ///   <item>
    ///     <see cref="ITenantDbIsolator"/> — default no-op implementation, suitable for
    ///     Shared database and Tenant-per-Database topologies. For Tenant-per-Schema,
    ///     register a custom <see cref="ITenantDbIsolator"/> with
    ///     <c>services.AddSingleton&lt;ITenantDbIsolator, MySchemaIsolator&gt;()</c>
    ///     <b>before</b> calling this method.
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// <c>RunMigrationBatchHandler</c> is auto-discovered by Wolverine from the assembly;
    /// no explicit handler registration is required.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configureProgressDb">
    /// Provider-specific <see cref="DbContextOptionsBuilder"/> configuration for the system
    /// migration progress database.
    /// Example: <c>opts =&gt; opts.UseNpgsql(connectionString)</c>
    /// </param>
    /// <returns>The builder, for chaining.</returns>
    public static IHostApplicationBuilder AddGranitPersistenceMigrations(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configureProgressDb)
    {
        // System DbContext — registered WITHOUT Wolverine EF Core transaction integration
        // so that progress commits are independent from the tenant data transaction.
        builder.Services.AddDbContext<MigrationProgressDbContext>(configureProgressDb);

        // Thread-safe singleton registry — populated at startup via Register<TContext>().
        builder.Services.TryAddSingleton<IMigrationCycleRegistry, MigrationCycleRegistry>();

        // Default no-op isolator. Applications using Tenant-per-Schema must register
        // their own ITenantDbIsolator BEFORE calling this method.
        builder.Services.TryAddSingleton<ITenantDbIsolator, NullTenantDbIsolator>();

        return builder;
    }
}
