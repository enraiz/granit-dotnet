using Granit.Templating.Store;
using Granit.Workflow.Definitions;
using Granit.Workflow.Domain;
using Granit.Workflow.EntityFrameworkCore;
using Granit.Workflow.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Templating.Workflow;

/// <summary>
/// Extension methods for registering the Granit.Templating.Workflow bridge.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default <see cref="NullTemplateTransitionHook"/> with a Workflow-aware
    /// implementation that provides FSM validation, approval routing, and unified HDS audit trail.
    /// </summary>
    /// <typeparam name="TDbContext">
    /// The host application's <see cref="DbContext"/> implementing <see cref="IWorkflowDbContext"/>.
    /// Must be registered via <c>AddDbContextFactory</c>.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitTemplatingWorkflow<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext, IWorkflowDbContext
    {
        services.AddGranitWorkflow();
        services.AddWorkflow(PublicationWorkflow.Default);
        services.Replace(ServiceDescriptor.Scoped<ITemplateTransitionHook, WorkflowTemplateTransitionHook<TDbContext>>());
        return services;
    }
}
