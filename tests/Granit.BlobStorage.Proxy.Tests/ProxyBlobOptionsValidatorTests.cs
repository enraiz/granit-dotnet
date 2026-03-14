using Granit.BlobStorage.Proxy.Options;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.Proxy.Tests;

public sealed class ProxyBlobOptionsValidatorTests
{
    private readonly ProxyBlobOptionsValidator _sut = new();

    [Fact]
    public void Valid_options_succeed()
    {
        ProxyBlobOptions options = new()
        {
            BaseUrl = "https://api.example.com",
            RoutePrefix = "/api/blobs",
            MaxUploadBytes = 104_857_600,
        };

        ValidateOptionsResult result = _sut.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_BaseUrl_fails(string? baseUrl)
    {
        ProxyBlobOptions options = new() { BaseUrl = baseUrl! };

        ValidateOptionsResult result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("BaseUrl");
    }

    [Fact]
    public void Invalid_BaseUrl_uri_fails()
    {
        ProxyBlobOptions options = new() { BaseUrl = "not-a-uri" };

        ValidateOptionsResult result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("absolute URI");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Non_positive_MaxUploadBytes_fails(long maxBytes)
    {
        ProxyBlobOptions options = new()
        {
            BaseUrl = "https://api.example.com",
            MaxUploadBytes = maxBytes,
        };

        ValidateOptionsResult result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("MaxUploadBytes");
    }

    [Theory]
    [InlineData("")]
    [InlineData("api/blobs")]
    [InlineData("blobs")]
    public void RoutePrefix_without_leading_slash_fails(string prefix)
    {
        ProxyBlobOptions options = new()
        {
            BaseUrl = "https://api.example.com",
            RoutePrefix = prefix,
        };

        ValidateOptionsResult result = _sut.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("RoutePrefix");
    }
}
