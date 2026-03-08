using Granit.DataExchange.Export;
using Granit.DataExchange.Export.Messages;
using Granit.DataExchange.Import.Domain;
using Granit.DataExchange.Import.Messages;
using Granit.DataExchange.Internal;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Tests.Export;

public sealed class NullDataExchangeEventPublisherTests
{
    private readonly NullDataExchangeEventPublisher _sut = new();

    [Fact]
    public async Task PublishAsync_ImportJobCompletedEvent_completes_without_error()
    {
        ImportJobCompletedEvent evt = new(
            Guid.NewGuid(), "Test.Import", ImportJobStatus.Completed,
            "user-1", 100, 95, 5, 90, 5, 0);

        Func<Task> act = () => _sut.PublishAsync(evt, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task PublishAsync_ExportJobCompletedEvent_completes_without_error()
    {
        ExportJobCompletedEvent evt = new(
            Guid.NewGuid(), "Test.Export", ExportJobStatus.Completed,
            "user-1", 200, null);

        Func<Task> act = () => _sut.PublishAsync(evt, TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(act);
    }
}
