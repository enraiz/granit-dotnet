using Granit.Core.MultiTenancy;
using Granit.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Wolverine.Postgresql.Internal;

/// <summary>
/// Scoped <see cref="IDbContextFactory{TContext}"/> that builds a <typeparamref name="TContext"/>
/// configured for the current tenant's isolated PostgreSQL database.
/// </summary>
/// <remarks>
/// <para>
/// Reads <see cref="ICurrentTenant.Id"/> — set by
/// <see cref="Granit.Wolverine.Behaviors.TenantContextBehavior"/> before the handler runs —
/// and delegates connection string resolution to <see cref="ITenantConnectionStringProvider"/>.
/// </para>
/// <para>
/// Throws <see cref="InvalidOperationException"/> when no tenant is active.
/// There is no silent fallback: accessing data without a tenant context would violate
/// HDS/RGPD inter-tenant isolation requirements.
/// </para>
/// <para>
/// <see cref="Granit.Persistence.Interceptors.AuditedEntityInterceptor"/> is wired automatically
/// when available in DI, satisfying the 3-year HDS audit trail requirement.
/// </para>
/// <para>
/// Prefer <see cref="CreateDbContextAsync"/> over <see cref="CreateDbContext"/>: the synchronous
/// overload uses <c>GetAwaiter().GetResult()</c> and is provided only for framework compatibility.
/// Connection strings are expected to be cached in memory by the provider.
/// </para>
/// </remarks>
/// <typeparam name="TContext">The tenant-specific <see cref="DbContext"/> type.</typeparam>
internal sealed class PerTenantDbContextFactory<TContext>(
    ICurrentTenant currentTenant,
    ITenantConnectionStringProvider connectionStringProvider,
    IServiceProvider serviceProvider) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ITenantConnectionStringProvider _connectionStringProvider = connectionStringProvider;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <inheritdoc/>
    public TContext CreateDbContext() =>
        CreateDbContextAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        Guid tenantId = _currentTenant.Id
            ?? throw new InvalidOperationException(
                "No active tenant context. Ensure TenantContextBehavior is registered " +
                "and the message carries the X-Tenant-Id header.");

        string connectionString = await _connectionStringProvider
            .GetConnectionStringAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);

        return BuildContext(connectionString);
    }

    private TContext BuildContext(string connectionString)
    {
        DbContextOptionsBuilder<TContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(connectionString);

        AuditedEntityInterceptor? auditInterceptor =
            _serviceProvider.GetService<AuditedEntityInterceptor>();

        if (auditInterceptor is not null)
        {
            optionsBuilder.AddInterceptors(auditInterceptor);
        }

        return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;
    }
}
