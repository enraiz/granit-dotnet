using Granit.DataExchange.Export;
using Granit.DataExchange.Export.Messages;
using Granit.DataExchange.Import.Domain;
using Granit.DataExchange.Import.Messages;
using Granit.DataExchange.Wolverine.Internal;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Wolverine;
using Xunit;

namespace Granit.DataExchange.Wolverine.Tests;

public sealed class WolverineDataExchangeEventPublisherTests
{
    private readonly IMessageBus _messageBus = Substitute.For<IMessageBus>();

    private WolverineDataExchangeEventPublisher CreatePublisher()
    {
        ServiceCollection services = new();
        services.AddScoped(_ => _messageBus);
        ServiceProvider sp = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        return new WolverineDataExchangeEventPublisher(scopeFactory);
    }

    [Fact]
    public async Task PublishAsync_ImportJobCompletedEvent_publishes_via_message_bus()
    {
        // Arrange
        WolverineDataExchangeEventPublisher publisher = CreatePublisher();
        ImportJobCompletedEvent evt = new(
            Guid.NewGuid(), "Test.Import", ImportJobStatus.Completed,
            "user-1", 100, 95, 5, 90, 5, 0);

        // Act
        await publisher.PublishAsync(evt, TestContext.Current.CancellationToken);

        // Assert
        await _messageBus.Received(1).PublishAsync(evt, Arg.Any<DeliveryOptions?>());
    }

    [Fact]
    public async Task PublishAsync_ExportJobCompletedEvent_publishes_via_message_bus()
    {
        // Arrange
        WolverineDataExchangeEventPublisher publisher = CreatePublisher();
        ExportJobCompletedEvent evt = new(
            Guid.NewGuid(), "Test.Export", ExportJobStatus.Completed,
            "user-1", 200, null);

        // Act
        await publisher.PublishAsync(evt, TestContext.Current.CancellationToken);

        // Assert
        await _messageBus.Received(1).PublishAsync(evt, Arg.Any<DeliveryOptions?>());
    }
}
