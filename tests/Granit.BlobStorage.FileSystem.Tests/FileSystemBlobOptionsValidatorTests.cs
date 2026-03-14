using Granit.BlobStorage.FileSystem.Options;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.FileSystem.Tests;

public sealed class FileSystemBlobOptionsValidatorTests
{
    private static readonly FileSystemBlobOptionsValidator Validator = new();

    [Fact]
    public void Validate_ValidBasePath_ReturnsSuccess()
    {
        FileSystemBlobOptions options = new() { BasePath = "./blobs" };

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeFalse();
    }

    [Fact]
    public void Validate_AbsoluteBasePath_ReturnsSuccess()
    {
        FileSystemBlobOptions options = new() { BasePath = "/data/blobs" };

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingBasePath_ReturnsFail(string basePath)
    {
        FileSystemBlobOptions options = new() { BasePath = basePath };

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(FileSystemBlobOptions.BasePath));
    }
}
