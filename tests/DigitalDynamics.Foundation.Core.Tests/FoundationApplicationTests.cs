// =============================================================================
// Tests - FoundationApplication
// =============================================================================
// Verifie que FoundationApplication :
//   - Appelle ConfigureServices sur chaque module dans l'ordre topologique
//   - Appelle OnApplicationInitialization dans l'ordre topologique
//   - Gere les modules sans override (no-op OK)
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DigitalDynamics.Foundation.Core.Tests;

public sealed class FoundationApplicationTests
{
    private static readonly List<string> CallOrder = [];

    // --- Modules de test avec tracking d'appels ---

    public sealed class TrackingModuleA : FoundationModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            CallOrder.Add("ConfigureServices:A");
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            CallOrder.Add("Initialize:A");
        }
    }

    [DependsOn(typeof(TrackingModuleA))]
    public sealed class TrackingModuleB : FoundationModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            CallOrder.Add("ConfigureServices:B");
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            CallOrder.Add("Initialize:B");
        }
    }

    // --- Module async qui surcharge uniquement ConfigureServicesAsync ---

    public sealed class AsyncTrackingModuleA : FoundationModule
    {
        public override async Task ConfigureServicesAsync(ServiceConfigurationContext context)
        {
            await Task.Delay(1); // Simule une operation async
            CallOrder.Add("ConfigureServicesAsync:A");
        }

        public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            await Task.Delay(1);
            CallOrder.Add("InitializeAsync:A");
        }
    }

    [DependsOn(typeof(AsyncTrackingModuleA))]
    public sealed class AsyncTrackingModuleB : FoundationModule
    {
        public override async Task ConfigureServicesAsync(ServiceConfigurationContext context)
        {
            await Task.Delay(1);
            CallOrder.Add("ConfigureServicesAsync:B");
        }

        public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            await Task.Delay(1);
            CallOrder.Add("InitializeAsync:B");
        }
    }

    public sealed class NoOpModule : FoundationModule;

    public FoundationApplicationTests()
    {
        CallOrder.Clear();
    }

    [Fact]
    public void ConfigureServices_CallsModulesInTopologicalOrder()
    {
        // Arrange
        var modules = ModuleLoader.LoadModules<TrackingModuleB>();
        var app = new FoundationApplication(modules);
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var context = new ServiceConfigurationContext(services, config, builder);

        // Act
        app.ConfigureServices(context);

        // Assert - A doit etre appele avant B
        CallOrder.Should().ContainInOrder("ConfigureServices:A", "ConfigureServices:B");
    }

    [Fact]
    public void InitializeApplication_CallsModulesInTopologicalOrder()
    {
        // Arrange
        var modules = ModuleLoader.LoadModules<TrackingModuleB>();
        var app = new FoundationApplication(modules);
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var context = new ApplicationInitializationContext(provider);

        // Act
        app.InitializeApplication(context);

        // Assert - A doit etre appele avant B
        CallOrder.Should().ContainInOrder("Initialize:A", "Initialize:B");
    }

    [Fact]
    public void ConfigureServices_NoOpModules_DoesNotThrow()
    {
        // Arrange
        var modules = ModuleLoader.LoadModules<NoOpModule>();
        var app = new FoundationApplication(modules);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var context = new ServiceConfigurationContext(
            new ServiceCollection(),
            new ConfigurationBuilder().Build(),
            builder);

        // Act
        var act = () => app.ConfigureServices(context);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ModuleTypes_ReturnsOrderedModuleTypes()
    {
        var modules = ModuleLoader.LoadModules<TrackingModuleB>();
        var app = new FoundationApplication(modules);

        var types = app.ModuleTypes;

        types.Should().HaveCount(2);
        types[0].Should().Be<TrackingModuleA>();
        types[1].Should().Be<TrackingModuleB>();
    }

    // --- Tests async ---

    [Fact]
    public async Task ConfigureServicesAsync_CallsModulesInTopologicalOrder()
    {
        // Arrange
        var modules = ModuleLoader.LoadModules<AsyncTrackingModuleB>();
        var app = new FoundationApplication(modules);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var context = new ServiceConfigurationContext(
            new ServiceCollection(),
            new ConfigurationBuilder().Build(),
            builder);

        // Act
        await app.ConfigureServicesAsync(context);

        // Assert - A doit etre appele avant B
        CallOrder.Should().ContainInOrder("ConfigureServicesAsync:A", "ConfigureServicesAsync:B");
    }

    [Fact]
    public async Task InitializeApplicationAsync_CallsModulesInTopologicalOrder()
    {
        // Arrange
        var modules = ModuleLoader.LoadModules<AsyncTrackingModuleB>();
        var app = new FoundationApplication(modules);
        var provider = new ServiceCollection().BuildServiceProvider();
        var context = new ApplicationInitializationContext(provider);

        // Act
        await app.InitializeApplicationAsync(context);

        // Assert - A doit etre appele avant B
        CallOrder.Should().ContainInOrder("InitializeAsync:A", "InitializeAsync:B");
    }

    [Fact]
    public async Task ConfigureServicesAsync_SyncOverride_CalledViaAsyncPath()
    {
        // Arrange - TrackingModuleA surcharge ConfigureServices (sync)
        // ConfigureServicesAsync par defaut appelle la version sync
        var modules = ModuleLoader.LoadModules<TrackingModuleB>();
        var app = new FoundationApplication(modules);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var context = new ServiceConfigurationContext(
            new ServiceCollection(),
            new ConfigurationBuilder().Build(),
            builder);

        // Act
        await app.ConfigureServicesAsync(context);

        // Assert - Les overrides sync sont appeles via le chemin async
        CallOrder.Should().ContainInOrder("ConfigureServices:A", "ConfigureServices:B");
    }

    [Fact]
    public async Task ConfigureServicesAsync_NoOpModules_DoesNotThrow()
    {
        // Arrange
        var modules = ModuleLoader.LoadModules<NoOpModule>();
        var app = new FoundationApplication(modules);
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var context = new ServiceConfigurationContext(
            new ServiceCollection(),
            new ConfigurationBuilder().Build(),
            builder);

        // Act
        var act = () => app.ConfigureServicesAsync(context);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
