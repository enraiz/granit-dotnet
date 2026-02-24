using Granit.Core.MultiTenancy;
using Granit.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Persistence.MultiTenancy;

/// <summary>
/// Scoped <see cref="IDbContextFactory{TContext}"/> that routes all EF Core queries
/// to the current tenant's dedicated PostgreSQL schema via <see cref="TenantSchemaConnectionInterceptor"/>.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="TenantPerDatabaseDbContextFactory{TContext}"/>, this factory uses a
/// shared database connection string. Physical isolation is achieved by switching the
/// PostgreSQL <c>search_path</c> at connection open time. The EF Core compiled model is
/// shared across all tenants — no per-tenant model recompilation, no memory leak.
/// </para>
/// <para>
/// Throws <see cref="InvalidOperationException"/> when no tenant is active.
/// There is no silent fallback: allowing a query to run without a <c>search_path</c>
/// override would expose data from a previously-pooled tenant connection (HDS breach).
/// </para>
/// <para>
/// <see cref="AuditedEntityInterceptor"/> is wired automatically when available in DI,
/// satisfying the 3-year HDS audit trail requirement.
/// </para>
/// </remarks>
/// <typeparam name="TContext">The <see cref="DbContext"/> type shared across tenants.</typeparam>
internal sealed class TenantPerSchemaDbContextFactory<TContext>(
    ICurrentTenant currentTenant,
    ITenantSchemaProvider schemaProvider,
    IServiceProvider serviceProvider,
    TenantPerSchemaDbContextOptions<TContext> options) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ITenantSchemaProvider _schemaProvider = schemaProvider;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly TenantPerSchemaDbContextOptions<TContext> _options = options;

    /// <inheritdoc/>
    public TContext CreateDbContext() =>
        CreateDbContextAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentTenant.IsAvailable)
        {
            throw new InvalidOperationException(
                "No active tenant context. Ensure the tenant is resolved before accessing " +
                "per-schema data (HTTP: TenantResolutionMiddleware; messaging: TenantContextBehavior).");
        }

        return BuildContext();
    }

    private TContext BuildContext()
    {
        DbContextOptionsBuilder<TContext> optionsBuilder = new();
        _options.Configure(optionsBuilder);

        TenantSchemaConnectionInterceptor schemaInterceptor = new(_currentTenant, _schemaProvider);
        optionsBuilder.AddInterceptors(schemaInterceptor);

        AuditedEntityInterceptor? auditInterceptor =
            _serviceProvider.GetService<AuditedEntityInterceptor>();

        if (auditInterceptor is not null)
        {
            optionsBuilder.AddInterceptors(auditInterceptor);
        }

        return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;
    }
}
