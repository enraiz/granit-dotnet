using Granit.Workflow.Notifications.Extensions;
using Granit.Workflow.Notifications.Internal;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Notifications.Tests;

public sealed class WorkflowNotificationsServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // AddGranitWorkflowNotifications
    // -------------------------------------------------------------------------

    [Fact]
    public void AddGranitWorkflowNotifications_RegistersNullApproverResolver()
    {
        ServiceCollection services = new();

        services.AddGranitWorkflowNotifications();

        using ServiceProvider sp = services.BuildServiceProvider();
        IApproverResolver resolver = sp.GetRequiredService<IApproverResolver>();

        resolver.ShouldBeOfType<NullApproverResolver>();
    }

    [Fact]
    public void AddGranitWorkflowNotifications_ReturnsServices_ForChaining()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddGranitWorkflowNotifications();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGranitWorkflowNotifications_DoesNotOverride_ExistingResolver()
    {
        ServiceCollection services = new();
        services.AddScoped<IApproverResolver, StubApproverResolver>();

        services.AddGranitWorkflowNotifications();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();
        IApproverResolver resolver = scope.ServiceProvider.GetRequiredService<IApproverResolver>();

        resolver.ShouldBeOfType<StubApproverResolver>();
    }

    // -------------------------------------------------------------------------
    // AddWorkflowApproverResolver<T>
    // -------------------------------------------------------------------------

    [Fact]
    public void AddWorkflowApproverResolver_ReplacesExistingRegistration()
    {
        ServiceCollection services = new();
        services.AddGranitWorkflowNotifications(); // registers NullApproverResolver

        services.AddWorkflowApproverResolver<StubApproverResolver>();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();
        IApproverResolver resolver = scope.ServiceProvider.GetRequiredService<IApproverResolver>();

        resolver.ShouldBeOfType<StubApproverResolver>();
    }

    [Fact]
    public void AddWorkflowApproverResolver_ReturnsServices_ForChaining()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddWorkflowApproverResolver<StubApproverResolver>();

        result.ShouldBeSameAs(services);
    }

    // -------------------------------------------------------------------------
    // AddIdentityApproverResolver
    // -------------------------------------------------------------------------

    [Fact]
    public void AddIdentityApproverResolver_ReturnsServices_ForChaining()
    {
        ServiceCollection services = new();

        IServiceCollection result = services.AddIdentityApproverResolver();

        result.ShouldBeSameAs(services);
    }

    // -------------------------------------------------------------------------
    // Stub
    // -------------------------------------------------------------------------

    private sealed class StubApproverResolver : IApproverResolver
    {
        public Task<IReadOnlyList<string>> ResolveApproversAsync(
            string requiredPermission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }
}
