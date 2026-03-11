using Granit.Templating.Store;
using Granit.Templating.Workflow.Internal;
using Granit.Workflow.Definitions;
using Granit.Workflow.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Templating.Workflow.Extensions;

/// <summary>
/// Extension methods for registering the Granit.Templating.Workflow bridge.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default <see cref="NullTemplateTransitionHook"/> with a Workflow-aware
    /// implementation that provides FSM validation, approval routing, and unified HDS audit trail.
    /// </summary>
    /// <remarks>
    /// <see cref="IWorkflowTransitionRecorder"/> must be registered separately,
    /// typically via <c>AddGranitWorkflowEntityFrameworkCore&lt;TDbContext&gt;()</c>
    /// from the <c>Granit.Workflow.EntityFrameworkCore</c> package.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitTemplatingWorkflow(
        this IServiceCollection services)
    {
        services.AddGranitWorkflow();
        services.AddWorkflow(PublicationWorkflow.Default);
        services.Replace(ServiceDescriptor.Scoped<ITemplateTransitionHook, WorkflowTemplateTransitionHook>());
        return services;
    }
}
