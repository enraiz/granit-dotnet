using Granit.BlobStorage.Internal;
using Granit.BlobStorage.Proxy.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.Proxy.Tests;

public sealed class ProxyEndpointsTests
{
    private readonly IBlobProxyTokenStore _tokenStore = Substitute.For<IBlobProxyTokenStore>();
    private readonly IBlobStoreProvider _storeProvider = Substitute.For<IBlobStoreProvider>();

    // ── Upload ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_valid_token_streams_to_provider_and_returns_204()
    {
        ProxyTokenEntry entry = new(
            ProxyTokenType.Upload, "bucket", "key", Guid.NewGuid(),
            Guid.NewGuid(), "application/pdf", 10_485_760, null);

        _tokenStore.ConsumeAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(entry);

        DefaultHttpContext context = CreateHttpContext(contentType: "application/pdf", contentLength: 1024);

        Results<NoContent, ProblemHttpResult> result = await ProxyEndpoints.HandleUploadAsync(
            "valid-token", context, _tokenStore, _storeProvider, TestContext.Current.CancellationToken);

        result.Result.ShouldBeOfType<NoContent>();

        await _storeProvider.Received(1).SaveAsync(
            "bucket", "key", context.Request.Body, "application/pdf", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_invalid_token_returns_403()
    {
        _tokenStore.ConsumeAsync("bad-token", Arg.Any<CancellationToken>())
            .Returns((ProxyTokenEntry?)null);

        DefaultHttpContext context = CreateHttpContext();

        Results<NoContent, ProblemHttpResult> result = await ProxyEndpoints.HandleUploadAsync(
            "bad-token", context, _tokenStore, _storeProvider, TestContext.Current.CancellationToken);

        ProblemHttpResult problem = result.Result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Upload_download_token_returns_403()
    {
        ProxyTokenEntry downloadEntry = new(
            ProxyTokenType.Download, "bucket", "key", Guid.Empty,
            Guid.NewGuid(), null, null, "file.pdf");

        _tokenStore.ConsumeAsync("download-token", Arg.Any<CancellationToken>())
            .Returns(downloadEntry);

        DefaultHttpContext context = CreateHttpContext();

        Results<NoContent, ProblemHttpResult> result = await ProxyEndpoints.HandleUploadAsync(
            "download-token", context, _tokenStore, _storeProvider, TestContext.Current.CancellationToken);

        ProblemHttpResult problem = result.Result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Upload_exceeding_max_bytes_returns_413()
    {
        ProxyTokenEntry entry = new(
            ProxyTokenType.Upload, "bucket", "key", Guid.NewGuid(),
            Guid.NewGuid(), "application/pdf", 1024, null); // max 1 KB

        _tokenStore.ConsumeAsync("token", Arg.Any<CancellationToken>())
            .Returns(entry);

        DefaultHttpContext context = CreateHttpContext(contentLength: 2048); // 2 KB — exceeds limit

        Results<NoContent, ProblemHttpResult> result = await ProxyEndpoints.HandleUploadAsync(
            "token", context, _tokenStore, _storeProvider, TestContext.Current.CancellationToken);

        ProblemHttpResult problem = result.Result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public async Task Upload_mismatched_content_type_returns_415()
    {
        ProxyTokenEntry entry = new(
            ProxyTokenType.Upload, "bucket", "key", Guid.NewGuid(),
            Guid.NewGuid(), "application/pdf", 10_485_760, null);

        _tokenStore.ConsumeAsync("token", Arg.Any<CancellationToken>())
            .Returns(entry);

        DefaultHttpContext context = CreateHttpContext(contentType: "image/png");

        Results<NoContent, ProblemHttpResult> result = await ProxyEndpoints.HandleUploadAsync(
            "token", context, _tokenStore, _storeProvider, TestContext.Current.CancellationToken);

        ProblemHttpResult problem = result.Result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status415UnsupportedMediaType);
    }

    [Fact]
    public async Task Upload_null_content_type_in_entry_accepts_any_request_content_type()
    {
        ProxyTokenEntry entry = new(
            ProxyTokenType.Upload, "bucket", "key", Guid.NewGuid(),
            Guid.NewGuid(), null, 10_485_760, null);

        _tokenStore.ConsumeAsync("token", Arg.Any<CancellationToken>())
            .Returns(entry);

        DefaultHttpContext context = CreateHttpContext(contentType: "image/png", contentLength: 512);

        Results<NoContent, ProblemHttpResult> result = await ProxyEndpoints.HandleUploadAsync(
            "token", context, _tokenStore, _storeProvider, TestContext.Current.CancellationToken);

        result.Result.ShouldBeOfType<NoContent>();
    }

    // ── Download ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Download_valid_token_streams_from_provider()
    {
        ProxyTokenEntry entry = new(
            ProxyTokenType.Download, "bucket", "key", Guid.Empty,
            Guid.NewGuid(), null, null, "report.pdf");

        _tokenStore.ConsumeAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(entry);

        MemoryStream stream = new([0x01, 0x02, 0x03]);
        _storeProvider.OpenReadAsync("bucket", "key", Arg.Any<CancellationToken>())
            .Returns(stream);

        Results<FileStreamHttpResult, ProblemHttpResult> result = await ProxyEndpoints.HandleDownloadAsync(
            "valid-token", _tokenStore, _storeProvider, TestContext.Current.CancellationToken);

        FileStreamHttpResult fileResult = result.Result.ShouldBeOfType<FileStreamHttpResult>();
        fileResult.ContentType.ShouldBe("application/octet-stream");
        fileResult.FileDownloadName.ShouldBe("report.pdf");
    }

    [Fact]
    public async Task Download_invalid_token_returns_403()
    {
        _tokenStore.ConsumeAsync("bad-token", Arg.Any<CancellationToken>())
            .Returns((ProxyTokenEntry?)null);

        Results<FileStreamHttpResult, ProblemHttpResult> result = await ProxyEndpoints.HandleDownloadAsync(
            "bad-token", _tokenStore, _storeProvider, TestContext.Current.CancellationToken);

        ProblemHttpResult problem = result.Result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Download_upload_token_returns_403()
    {
        ProxyTokenEntry uploadEntry = new(
            ProxyTokenType.Upload, "bucket", "key", Guid.NewGuid(),
            Guid.NewGuid(), "application/pdf", 10_485_760, null);

        _tokenStore.ConsumeAsync("upload-token", Arg.Any<CancellationToken>())
            .Returns(uploadEntry);

        Results<FileStreamHttpResult, ProblemHttpResult> result = await ProxyEndpoints.HandleDownloadAsync(
            "upload-token", _tokenStore, _storeProvider, TestContext.Current.CancellationToken);

        ProblemHttpResult problem = result.Result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DefaultHttpContext CreateHttpContext(
        string? contentType = null,
        long? contentLength = null)
    {
        DefaultHttpContext context = new();
        if (contentType is not null)
        {
            context.Request.ContentType = contentType;
        }

        if (contentLength is not null)
        {
            context.Request.ContentLength = contentLength;
        }

        context.Request.Body = new MemoryStream();
        return context;
    }
}
