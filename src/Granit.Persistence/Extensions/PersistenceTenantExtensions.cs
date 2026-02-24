using Granit.Persistence.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
}
