using Granit.Workflow.EntityFrameworkCore.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Workflow.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for registering Granit.Workflow EF Core services.
/// </summary>
public static class WorkflowEfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="WorkflowTransitionInterceptor"/> as a scoped service
    /// for automatic HDS audit trail creation on workflow state transitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The host application's DbContext must implement <see cref="IWorkflowDbContext"/>
    /// and call <c>modelBuilder.ConfigureWorkflowModule()</c> in <c>OnModelCreating</c>.
    /// </para>
    /// <para>
    /// The interceptor must be ordered after <c>AuditedEntityInterceptor</c> and before
    /// <c>SoftDeleteInterceptor</c> in the interceptor chain.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitWorkflowEntityFrameworkCore(
        this IServiceCollection services)
    {
        services.TryAddScoped<WorkflowTransitionInterceptor>();
        return services;
    }
}
