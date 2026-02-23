// =============================================================================
// Tests - Integration AddGranit<T> / UseGranit
// =============================================================================
// Verifie le pipeline complet :
//   - AddGranit<T>() enregistre GranitApplication en singleton
//   - Les services enregistres par les modules sont resolus
//   - UseGranit() appelle OnApplicationInitialization
// =============================================================================

using FluentAssertions;
using Granit.Core.Extensions;
using Granit.Core.Modularity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Granit.Core.Tests;

public sealed class IntegrationTests
{
    // --- Modules de test avec services ---

    public interface ITestService;
    private sealed class TestServiceImpl : ITestService;

    public sealed class TestLeafModule : GranitModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context) => context.Services.AddSingleton<ITestService, TestServiceImpl>();
    }

    private static bool _initializationCalled;
    private static bool _asyncInitializationCalled;

    [DependsOn(typeof(TestLeafModule))]
    public sealed class TestRootModule : GranitModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context) => _initializationCalled = true;
    }

    // --- Modules async ---

    public sealed class AsyncTestLeafModule : GranitModule
    {
        public override async Task ConfigureServicesAsync(ServiceConfigurationContext context)
        {
            await Task.Delay(1);
            context.Services.AddSingleton<ITestService, TestServiceImpl>();
        }
    }

    [DependsOn(typeof(AsyncTestLeafModule))]
    public sealed class AsyncTestRootModule : GranitModule
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
    public void AddGranit_RegistersGranitApplicationAsSingleton()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddGranit<TestRootModule>();
        using var app = builder.Build();

        // Assert
        var foundationApp = app.Services.GetService<GranitApplication>();
        foundationApp.Should().NotBeNull();

        var secondResolve = app.Services.GetService<GranitApplication>();
        secondResolve.Should().BeSameAs(foundationApp);
    }

    [Fact]
    public void AddGranit_ModuleServicesAreRegistered()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddGranit<TestRootModule>();
        using var app = builder.Build();

        // Assert - TestLeafModule enregistre ITestService
        var service = app.Services.GetService<ITestService>();
        service.Should().NotBeNull();
    }

    [Fact]
    public void UseGranit_CallsOnApplicationInitialization()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.AddGranit<TestRootModule>();
        using var app = builder.Build();

        // Act
        app.UseGranit();

        // Assert
        _initializationCalled.Should().BeTrue();
    }

    [Fact]
    public void AddGranit_ModuleTypesAreInTopologicalOrder()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddGranit<TestRootModule>();
        using var app = builder.Build();

        // Assert
        var foundationApp = app.Services.GetRequiredService<GranitApplication>();
        foundationApp.GetModuleTypes().Should().ContainInOrder(
            typeof(TestLeafModule),
            typeof(TestRootModule));
    }

    // --- Tests async ---

    [Fact]
    public async Task AddGranitAsync_RegistersGranitApplicationAsSingleton()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        await builder.AddGranitAsync<AsyncTestRootModule>();
        await using var app = builder.Build();

        // Assert
        var foundationApp = app.Services.GetService<GranitApplication>();
        foundationApp.Should().NotBeNull();

        var secondResolve = app.Services.GetService<GranitApplication>();
        secondResolve.Should().BeSameAs(foundationApp);
    }

    [Fact]
    public async Task AddGranitAsync_ModuleServicesAreRegistered()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        await builder.AddGranitAsync<AsyncTestRootModule>();
        await using var app = builder.Build();

        // Assert - AsyncTestLeafModule enregistre ITestService via ConfigureServicesAsync
        var service = app.Services.GetService<ITestService>();
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task UseGranitAsync_CallsOnApplicationInitializationAsync()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        await builder.AddGranitAsync<AsyncTestRootModule>();
        await using var app = builder.Build();

        // Act
        await app.UseGranitAsync();

        // Assert
        _asyncInitializationCalled.Should().BeTrue();
    }

    [Fact]
    public async Task AddGranitAsync_ModuleTypesAreInTopologicalOrder()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        await builder.AddGranitAsync<AsyncTestRootModule>();
        await using var app = builder.Build();

        // Assert
        var foundationApp = app.Services.GetRequiredService<GranitApplication>();
        foundationApp.GetModuleTypes().Should().ContainInOrder(
            typeof(AsyncTestLeafModule),
            typeof(AsyncTestRootModule));
    }
}
