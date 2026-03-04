using Granit.DataExchange.Import.Internal;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Tests.Import;

public sealed class NullImportFileProviderTests
{
    private readonly NullImportFileProvider _provider = new();

    [Fact]
    public async Task OpenAsync_ThrowsNotImplementedException()
    {
        await Should.ThrowAsync<NotImplementedException>(() =>
            _provider.OpenAsync("ref", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_ThrowsNotImplementedException()
    {
        await Should.ThrowAsync<NotImplementedException>(() =>
            _provider.SaveAsync("file.csv", Stream.Null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotImplementedException()
    {
        await Should.ThrowAsync<NotImplementedException>(() =>
            _provider.DeleteAsync("ref", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExceptionMessage_ContainsGuidance()
    {
        NotImplementedException ex = await Should.ThrowAsync<NotImplementedException>(() =>
            _provider.OpenAsync("ref", TestContext.Current.CancellationToken));

        ex.Message.ShouldContain("IImportFileProvider");
    }
}
