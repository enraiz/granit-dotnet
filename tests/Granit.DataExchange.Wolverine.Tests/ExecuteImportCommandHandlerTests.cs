using Granit.DataExchange.Import.Messages;
using Granit.DataExchange.Import.Pipeline;
using Granit.DataExchange.Import.Reporting;
using Granit.DataExchange.Wolverine.Internal;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Wolverine.Tests;

public sealed class ExecuteImportCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_delegates_to_orchestrator()
    {
        // Arrange
        IImportOrchestrator orchestrator = Substitute.For<IImportOrchestrator>();
        var jobId = Guid.NewGuid();
        ExecuteImportCommand command = new(jobId, "Test.Import");
        ExecuteImportCommandHandler handler = new(orchestrator);

        // Act
        await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        await orchestrator.Received(1).ExecuteAsync(jobId, TestContext.Current.CancellationToken);
    }
}
