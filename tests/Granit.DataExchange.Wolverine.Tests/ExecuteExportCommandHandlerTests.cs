using Granit.DataExchange.Export;
using Granit.DataExchange.Export.Messages;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Wolverine.Tests;

public sealed class ExecuteExportCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_delegates_to_orchestrator()
    {
        // Arrange
        IExportOrchestrator orchestrator = Substitute.For<IExportOrchestrator>();
        Guid jobId = Guid.NewGuid();
        ExecuteExportCommand command = new(jobId);
        ExecuteExportCommandHandler handler = new(orchestrator);

        // Act
        await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        await orchestrator.Received(1).ExecuteAsync(jobId, TestContext.Current.CancellationToken);
    }
}
