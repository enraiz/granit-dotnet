using Granit.Persistence.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Granit.Persistence.Extensions;

/// <summary>
/// Extension methods for registering Granit per-tenant data isolation services.
/// </summary>
public static class PersistenceTenantExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TContext"/> as a per-tenant <see cref="IDbContextFactory{TContext}"/>:
    /// each call to <c>CreateDbContextAsync()</c> resolves the connection string from
    /// <see cref="ITenantConnectionStringProvider"/> using the current tenant context.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">
    /// Configures the <see cref="DbContextOptionsBuilder{TContext}"/> with the resolved
    /// connection string. Typically: <c>(opts, cs) =&gt; opts.UseNpgsql(cs)</c>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Requires <see cref="ITenantConnectionStringProvider"/> to be registered in DI before
    /// the host is built. A missing registration causes a runtime exception on the first
    /// <c>CreateDbContextAsync()</c> call (fail-fast behaviour).
    /// </para>
    /// <para>
    /// Both <see cref="IDbContextFactory{TContext}"/> and <typeparamref name="TContext"/>
    /// are registered as <see cref="ServiceLifetime.Scoped"/>.
    /// <c>TryAdd</c> semantics are used so that integration tests can pre-register a
    /// custom factory without being overridden.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddTenantPerDatabaseDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder<TContext>, string> configureOptions)
        where TContext : DbContext
    {
        services.AddSingleton(new TenantPerDatabaseDbContextOptions<TContext>
        {
            Configure = configureOptions,
        });

        services.TryAddScoped<IDbContextFactory<TContext>,
            TenantPerDatabaseDbContextFactory<TContext>>();

        services.TryAddScoped<TContext>(
            static sp => sp.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TContext"/> as a per-schema <see cref="IDbContextFactory{TContext}"/>:
    /// each connection is routed to the current tenant's dedicated PostgreSQL schema via
    /// <c>SET search_path TO {schema}, public</c>, executed unconditionally at connection open.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">
    /// Configures the <see cref="DbContextOptionsBuilder{TContext}"/> with the shared
    /// connection string. Typically: <c>opts =&gt; opts.UseNpgsql(sharedConnectionString)</c>.
    /// Do not set <c>search_path</c> here — it is managed by
    /// <see cref="TenantSchemaConnectionInterceptor"/>.
    /// </param>
    /// <param name="configureTenantSchema">
    /// Optional action to configure <see cref="TenantSchemaOptions"/> (prefix, naming convention).
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Connection pool safety</strong> — <see cref="TenantSchemaConnectionInterceptor"/>
    /// runs unconditionally on every connection lease from the Npgsql pool, overwriting any
    /// previous tenant's <c>search_path</c>. No bypass condition exists.
    /// </para>
    /// <para>
    /// If no custom <see cref="ITenantSchemaProvider"/> is registered, the default
    /// <see cref="DefaultTenantSchemaProvider"/> is used (convention from
    /// <see cref="TenantSchemaOptions"/>).
    /// </para>
    /// </remarks>
    public static IServiceCollection AddTenantPerSchemaDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder<TContext>> configureOptions,
        Action<TenantSchemaOptions>? configureTenantSchema = null)
        where TContext : DbContext
    {
        services.AddOptions<TenantSchemaOptions>()
            .Configure(configureTenantSchema ?? (_ => { }));

        services.TryAddSingleton<ITenantSchemaProvider, DefaultTenantSchemaProvider>();

        services.AddSingleton(new TenantPerSchemaDbContextOptions<TContext>
        {
            Configure = configureOptions,
        });

        services.TryAddScoped<IDbContextFactory<TContext>,
            TenantPerSchemaDbContextFactory<TContext>>();

        services.TryAddScoped<TContext>(
            static sp => sp.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());

        return services;
    }
}
