using Granit.Authorization;
using Granit.Authorization.Abstractions;
using Granit.Core.Modularity;
using Granit.Validation.Extensions;
using Granit.Workflow.Endpoints.Permissions;
using Granit.Workflow.Endpoints.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Workflow.Endpoints;

/// <summary>
/// Granit module for workflow HTTP endpoints.
/// Exposes workflow transition history and status via Minimal API routes.
/// </summary>
/// <remarks>
/// <para>
/// Map endpoints in your application:
/// <code>
/// app.MapWorkflowEndpoints();
/// </code>
/// </para>
/// <para>
/// Register services:
/// <code>
/// services.AddGranitWorkflowEndpoints();
/// </code>
/// </para>
/// </remarks>
[DependsOn(
    typeof(GranitWorkflowModule),
    typeof(GranitAuthorizationModule))]
public sealed class GranitWorkflowEndpointsModule : GranitModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IPermissionDefinitionProvider,
            WorkflowPermissionDefinitionProvider>();
        context.Services.AddGranitValidatorsFromAssemblyContaining<WorkflowTransitionRequestValidator>();
    }
}
