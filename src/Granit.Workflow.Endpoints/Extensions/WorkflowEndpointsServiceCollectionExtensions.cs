using Granit.Workflow.Endpoints.Internal;
using Granit.Workflow.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Workflow.Endpoints.Extensions;

/// <summary>
/// Extension methods for registering Granit.Workflow.Endpoints services.
/// </summary>
public static class WorkflowEndpointsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the workflow endpoints services including the
    /// <see cref="IWorkflowHistoryQuery"/> implementation backed by the host DbContext.
    /// </summary>
    /// <typeparam name="TDbContext">
    /// The host application's DbContext implementing <see cref="IWorkflowDbContext"/>.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitWorkflowEndpoints<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext, IWorkflowDbContext
    {
        services.TryAddScoped<IWorkflowHistoryQuery, DefaultWorkflowHistoryQuery<TDbContext>>();
        return services;
    }
}
