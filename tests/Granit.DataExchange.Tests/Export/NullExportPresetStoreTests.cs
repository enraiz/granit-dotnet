using Granit.DataExchange.Export;
using Granit.DataExchange.Export.Internal;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Tests.Export;

public sealed class NullExportPresetStoreTests
{
    private readonly NullExportPresetStore _store = new();

    [Fact]
    public async Task GetAsync_ReturnsNull()
    {
        ExportPreset? result = await _store.GetAsync(
            "def", "preset", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyList()
    {
        IReadOnlyList<ExportPreset> result = await _store.ListAsync(
            "def", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_DoesNotThrow()
    {
        ExportPreset preset = new("Test", "Default", [], "csv", false);

        await Should.NotThrowAsync(() =>
            _store.SaveAsync(preset, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_DoesNotThrow() =>
        await Should.NotThrowAsync(() =>
            _store.DeleteAsync("def", "preset", TestContext.Current.CancellationToken));
}
