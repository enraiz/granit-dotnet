using Granit.Core.Modularity;
using Granit.DataExchange.Export;
using Granit.DataExchange.Import.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Wolverine.Tests;

public sealed class GranitDataExchangeWolverineModuleTests
{
    [Fact]
    public void ConfigureServices_replaces_import_dispatcher()
    {
        // Arrange
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddSingleton<IImportCommandDispatcher, StubImportDispatcher>();
        builder.Services.AddSingleton<IExportCommandDispatcher, StubExportDispatcher>();

        ServiceConfigurationContext context = new(builder.Services, builder.Configuration, builder);
        GranitDataExchangeWolverineModule module = new();

        // Act
        module.ConfigureServices(context);

        // Assert
        ServiceDescriptor? descriptor = builder.Services.FirstOrDefault(
            d => d.ServiceType == typeof(IImportCommandDispatcher));
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(WolverineImportCommandDispatcher));
    }

    [Fact]
    public void ConfigureServices_replaces_export_dispatcher()
    {
        // Arrange
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddSingleton<IImportCommandDispatcher, StubImportDispatcher>();
        builder.Services.AddSingleton<IExportCommandDispatcher, StubExportDispatcher>();

        ServiceConfigurationContext context = new(builder.Services, builder.Configuration, builder);
        GranitDataExchangeWolverineModule module = new();

        // Act
        module.ConfigureServices(context);

        // Assert
        ServiceDescriptor? descriptor = builder.Services.FirstOrDefault(
            d => d.ServiceType == typeof(IExportCommandDispatcher));
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(WolverineExportCommandDispatcher));
    }

    [Fact]
    public void ConfigureServices_replaces_event_publisher()
    {
        // Arrange
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services.AddSingleton<IImportCommandDispatcher, StubImportDispatcher>();
        builder.Services.AddSingleton<IExportCommandDispatcher, StubExportDispatcher>();
        builder.Services.AddSingleton<IDataExchangeEventPublisher, StubEventPublisher>();

        ServiceConfigurationContext context = new(builder.Services, builder.Configuration, builder);
        GranitDataExchangeWolverineModule module = new();

        // Act
        module.ConfigureServices(context);

        // Assert
        ServiceDescriptor? descriptor = builder.Services.FirstOrDefault(
            d => d.ServiceType == typeof(IDataExchangeEventPublisher));
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(WolverineDataExchangeEventPublisher));
    }

    private sealed class StubEventPublisher : IDataExchangeEventPublisher
    {
        public Task PublishAsync(Import.Messages.ImportJobCompletedEvent notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PublishAsync(Export.Messages.ExportJobCompletedEvent notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubImportDispatcher : IImportCommandDispatcher
    {
        public Task DispatchAsync(Import.Messages.ExecuteImportCommand command, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubExportDispatcher : IExportCommandDispatcher
    {
        public Task DispatchAsync(Export.Messages.ExecuteExportCommand command, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
