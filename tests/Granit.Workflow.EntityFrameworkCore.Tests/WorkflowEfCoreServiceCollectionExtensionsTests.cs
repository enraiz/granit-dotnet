using Granit.Workflow.EntityFrameworkCore.Extensions;
using Granit.Workflow.EntityFrameworkCore.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Workflow.EntityFrameworkCore.Tests;

/// <summary>
/// Tests for <see cref="WorkflowEfCoreServiceCollectionExtensions"/>.
/// </summary>
public sealed class WorkflowEfCoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitWorkflowEntityFrameworkCore_ShouldRegisterInterceptor()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitWorkflowEntityFrameworkCore();

        // Assert — the interceptor requires its dependencies, so we check registration
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(WorkflowTransitionInterceptor));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddGranitWorkflowEntityFrameworkCore_ShouldNotOverrideExistingRegistration()
    {
        // Arrange
        ServiceCollection services = new();

        // Act — register twice
        services.AddGranitWorkflowEntityFrameworkCore();
        services.AddGranitWorkflowEntityFrameworkCore();

        // Assert — TryAddScoped should prevent duplicate
        int count = services.Count(d => d.ServiceType == typeof(WorkflowTransitionInterceptor));
        count.ShouldBe(1);
    }

    [Fact]
    public void AddGranitWorkflowEntityFrameworkCore_ShouldReturnSameServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddGranitWorkflowEntityFrameworkCore();

        // Assert
        result.ShouldBeSameAs(services);
    }
}
