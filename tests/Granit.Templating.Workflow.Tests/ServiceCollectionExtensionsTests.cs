using Granit.Templating.Extensions;
using Granit.Templating.Store;
using Granit.Templating.Workflow.Extensions;
using Granit.Workflow;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Templating.Workflow.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitTemplatingWorkflow_Replaces_Hook()
    {
        ServiceCollection services = new();
        services.AddGranitTemplating();

        // Register the IWorkflowTransitionRecorder that the hook requires
        services.AddSingleton(Substitute.For<IWorkflowTransitionRecorder>());

        services.AddGranitTemplatingWorkflow();

        ServiceDescriptor? hookDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ITemplateTransitionHook));

        hookDescriptor.ShouldNotBeNull();
        hookDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        hookDescriptor.ImplementationType.ShouldBe(
            typeof(WorkflowTemplateTransitionHook));
    }
}
