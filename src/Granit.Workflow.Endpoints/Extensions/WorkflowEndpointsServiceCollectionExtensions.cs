using Microsoft.Extensions.DependencyInjection;

namespace Granit.Workflow.Endpoints.Extensions;

/// <summary>
/// Extension methods for registering Granit.Workflow.Endpoints services.
/// </summary>
public static class WorkflowEndpointsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the workflow endpoints services.
    /// </summary>
    /// <remarks>
    /// <see cref="IWorkflowHistoryQuery"/> must be registered separately,
    /// typically via <c>AddGranitWorkflowEntityFrameworkCore&lt;TDbContext&gt;()</c>
    /// from the <c>Granit.Workflow.EntityFrameworkCore</c> package.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitWorkflowEndpoints(
        this IServiceCollection services) =>
        services;
}
