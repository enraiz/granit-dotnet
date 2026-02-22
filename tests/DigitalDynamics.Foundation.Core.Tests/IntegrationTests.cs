// =============================================================================
// Tests - Integration AddFoundation<T> / UseFoundation
// =============================================================================
// Verifie le pipeline complet :
//   - AddFoundation<T>() enregistre FoundationApplication en singleton
//   - Les services enregistres par les modules sont resolus
//   - UseFoundation() appelle OnApplicationInitialization
// =============================================================================

using DigitalDynamics.Foundation.Core.Extensions;
using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DigitalDynamics.Foundation.Core.Tests;

public sealed class IntegrationTests
{
    // --- Modules de test avec services ---

    public interface ITestService;
    private sealed class TestServiceImpl : ITestService;

    public sealed class TestLeafModule : FoundationModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context) => context.Services.AddSingleton<ITestService, TestServiceImpl>();
    }

    private static bool _initializationCalled;
    private static bool _asyncInitializationCalled;

    [DependsOn(typeof(TestLeafModule))]
    public sealed class TestRootModule : FoundationModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context) => _initializationCalled = true;
    }

    // --- Modules async ---

    public sealed class AsyncTestLeafModule : FoundationModule
    {
        public override async Task ConfigureServicesAsync(ServiceConfigurationContext context)
        {
            await Task.Delay(1);
            context.Services.AddSingleton<ITestService, TestServiceImpl>();
        }
    }

    [DependsOn(typeof(AsyncTestLeafModule))]
    public sealed class AsyncTestRootModule : FoundationModule
    {
        public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            await Task.Delay(1);
            _asyncInitializationCalled = true;
        }
    }

    public IntegrationTests()
    {
        _initializationCalled = false;
        _asyncInitializationCalled = false;
    }

    [Fact]
    public void AddFoundation_RegistersFoundationApplicationAsSingleton()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddFoundation<TestRootModule>();
        using var app = builder.Build();

        // Assert
        var foundationApp = app.Services.GetService<FoundationApplication>();
        foundationApp.Should().NotBeNull();

        var secondResolve = app.Services.GetService<FoundationApplication>();
        secondResolve.Should().BeSameAs(foundationApp);
    }

    [Fact]
    public void AddFoundation_ModuleServicesAreRegistered()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddFoundation<TestRootModule>();
        using var app = builder.Build();

        // Assert - TestLeafModule enregistre ITestService
        var service = app.Services.GetService<ITestService>();
        service.Should().NotBeNull();
    }

    [Fact]
    public void UseFoundation_CallsOnApplicationInitialization()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.AddFoundation<TestRootModule>();
        using var app = builder.Build();

        // Act
        app.UseFoundation();

        // Assert
        _initializationCalled.Should().BeTrue();
    }

    [Fact]
    public void AddFoundation_ModuleTypesAreInTopologicalOrder()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddFoundation<TestRootModule>();
        using var app = builder.Build();

        // Assert
        var foundationApp = app.Services.GetRequiredService<FoundationApplication>();
        foundationApp.GetModuleTypes().Should().ContainInOrder(
            typeof(TestLeafModule),
            typeof(TestRootModule));
    }

    // --- Tests async ---

    [Fact]
    public async Task AddFoundationAsync_RegistersFoundationApplicationAsSingleton()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        await builder.AddFoundationAsync<AsyncTestRootModule>();
        await using var app = builder.Build();

        // Assert
        var foundationApp = app.Services.GetService<FoundationApplication>();
        foundationApp.Should().NotBeNull();

        var secondResolve = app.Services.GetService<FoundationApplication>();
        secondResolve.Should().BeSameAs(foundationApp);
    }

    [Fact]
    public async Task AddFoundationAsync_ModuleServicesAreRegistered()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        await builder.AddFoundationAsync<AsyncTestRootModule>();
        await using var app = builder.Build();

        // Assert - AsyncTestLeafModule enregistre ITestService via ConfigureServicesAsync
        var service = app.Services.GetService<ITestService>();
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task UseFoundationAsync_CallsOnApplicationInitializationAsync()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        await builder.AddFoundationAsync<AsyncTestRootModule>();
        await using var app = builder.Build();

        // Act
        await app.UseFoundationAsync();

        // Assert
        _asyncInitializationCalled.Should().BeTrue();
    }

    [Fact]
    public async Task AddFoundationAsync_ModuleTypesAreInTopologicalOrder()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        await builder.AddFoundationAsync<AsyncTestRootModule>();
        await using var app = builder.Build();

        // Assert
        var foundationApp = app.Services.GetRequiredService<FoundationApplication>();
        foundationApp.GetModuleTypes().Should().ContainInOrder(
            typeof(AsyncTestLeafModule),
            typeof(AsyncTestRootModule));
    }
}
