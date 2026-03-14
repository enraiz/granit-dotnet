using Granit.BlobStorage.Proxy.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.Proxy.Tests;

public sealed class DistributedBlobProxyTokenStoreTests
{
    private readonly IDistributedCache _cache = new MemoryDistributedCache(
        Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));

    private readonly DistributedBlobProxyTokenStore _sut;

    public DistributedBlobProxyTokenStoreTests()
    {
        _sut = new DistributedBlobProxyTokenStore(_cache);
    }

    private static ProxyTokenEntry CreateUploadEntry(
        Guid? tenantId = null,
        string bucket = "test-bucket",
        string objectKey = "tenants/abc/file.pdf") =>
        new(
            Type: ProxyTokenType.Upload,
            Bucket: bucket,
            ObjectKey: objectKey,
            BlobId: Guid.NewGuid(),
            TenantId: tenantId ?? Guid.NewGuid(),
            ContentType: "application/pdf",
            MaxBytes: 10_485_760,
            DownloadFileName: null);

    private static ProxyTokenEntry CreateDownloadEntry(
        string? downloadFileName = "report.pdf") =>
        new(
            Type: ProxyTokenType.Download,
            Bucket: "test-bucket",
            ObjectKey: "tenants/abc/file.pdf",
            BlobId: Guid.Empty,
            TenantId: Guid.NewGuid(),
            ContentType: null,
            MaxBytes: null,
            DownloadFileName: downloadFileName);

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_returns_base64url_token_of_22_chars()
    {
        ProxyTokenEntry entry = CreateUploadEntry();

        string token = await _sut.CreateAsync(entry, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

        token.ShouldNotBeNullOrWhiteSpace();
        token.Length.ShouldBe(22); // 128-bit → base64url = 22 chars (no padding)
        token.ShouldNotContain("+");
        token.ShouldNotContain("/");
        token.ShouldNotContain("=");
    }

    [Fact]
    public async Task CreateAsync_generates_unique_tokens()
    {
        ProxyTokenEntry entry = CreateUploadEntry();

        string token1 = await _sut.CreateAsync(entry, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        string token2 = await _sut.CreateAsync(entry, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

        token1.ShouldNotBe(token2);
    }

    // ── ConsumeAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ConsumeAsync_returns_entry_and_removes_token()
    {
        ProxyTokenEntry entry = CreateUploadEntry();
        string token = await _sut.CreateAsync(entry, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

        // First consume should return the entry.
        ProxyTokenEntry? consumed = await _sut.ConsumeAsync(token, TestContext.Current.CancellationToken);
        consumed.ShouldNotBeNull();
        consumed.Bucket.ShouldBe(entry.Bucket);
        consumed.ObjectKey.ShouldBe(entry.ObjectKey);
        consumed.BlobId.ShouldBe(entry.BlobId);
        consumed.TenantId.ShouldBe(entry.TenantId);
        consumed.ContentType.ShouldBe(entry.ContentType);
        consumed.MaxBytes.ShouldBe(entry.MaxBytes);
        consumed.Type.ShouldBe(ProxyTokenType.Upload);

        // Second consume — single-use: should return null.
        ProxyTokenEntry? secondConsume = await _sut.ConsumeAsync(token, TestContext.Current.CancellationToken);
        secondConsume.ShouldBeNull();
    }

    [Fact]
    public async Task ConsumeAsync_returns_null_for_unknown_token()
    {
        ProxyTokenEntry? result = await _sut.ConsumeAsync("nonexistent-token", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ConsumeAsync_preserves_download_entry_fields()
    {
        ProxyTokenEntry entry = CreateDownloadEntry("report.xlsx");
        string token = await _sut.CreateAsync(entry, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

        ProxyTokenEntry? consumed = await _sut.ConsumeAsync(token, TestContext.Current.CancellationToken);

        consumed.ShouldNotBeNull();
        consumed.Type.ShouldBe(ProxyTokenType.Download);
        consumed.DownloadFileName.ShouldBe("report.xlsx");
        consumed.ContentType.ShouldBeNull();
        consumed.MaxBytes.ShouldBeNull();
    }
}
