using Granit.BackgroundJobs.Abstractions;
using Granit.BackgroundJobs.Internal;
using Shouldly;
using Xunit;

namespace Granit.BackgroundJobs.Tests.Internal;

public sealed class NullDeadLetterQueueInspectorTests
{
    private readonly NullDeadLetterQueueInspector _inspector = new();

    [Fact]
    public async Task GetCountsAsync_ReturnsEmptyDictionary()
    {
        IReadOnlyDictionary<string, long> result = await _inspector.GetCountsAsync(
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCountsAsync_MultipleCalls_ReturnsSameEmptyDictionary()
    {
        IReadOnlyDictionary<string, long> first = await _inspector.GetCountsAsync(
            TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, long> second = await _inspector.GetCountsAsync(
            TestContext.Current.CancellationToken);

        first.ShouldBeEmpty();
        second.ShouldBeEmpty();
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void ImplementsIDeadLetterQueueInspector() =>
        _inspector.ShouldBeAssignableTo<IDeadLetterQueueInspector>();
}
