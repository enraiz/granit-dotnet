using Granit.Templating.Store;
using Granit.Workflow.Domain;
using Granit.Workflow.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Templating.Workflow.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    private sealed class FakeWorkflowDbContext(DbContextOptions<FakeWorkflowDbContext> options)
        : DbContext(options), IWorkflowDbContext
    {
        public DbSet<WorkflowTransitionRecord> WorkflowTransitionRecords => Set<WorkflowTransitionRecord>();
    }

    [Fact]
    public void AddGranitTemplatingWorkflow_Replaces_Hook()
    {
        ServiceCollection services = new();
        services.AddGranitTemplating();
        services.AddDbContextFactory<FakeWorkflowDbContext>(o => o.UseInMemoryDatabase("test"));
        services.AddGranitTemplatingWorkflow<FakeWorkflowDbContext>();

        ServiceDescriptor? hookDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ITemplateTransitionHook));

        hookDescriptor.ShouldNotBeNull();
        hookDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        hookDescriptor.ImplementationType.ShouldBe(
            typeof(WorkflowTemplateTransitionHook<FakeWorkflowDbContext>));
    }
}
