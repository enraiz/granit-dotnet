using Granit.BlobStorage.Database.Options;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.Database.Tests;

public sealed class DatabaseBlobOptionsValidatorTests
{
    private static readonly DatabaseBlobOptionsValidator Validator = new();

    [Fact]
    public void Validate_DefaultOptions_ReturnsSuccess()
    {
        DatabaseBlobOptions options = new();

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeFalse();
    }

    [Fact]
    public void Validate_CustomMaxSize_ReturnsSuccess()
    {
        DatabaseBlobOptions options = new() { MaxBlobSizeBytes = 5 * 1024 * 1024 };

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_ZeroOrNegativeMaxSize_ReturnsFail(long maxSize)
    {
        DatabaseBlobOptions options = new() { MaxBlobSizeBytes = maxSize };

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(DatabaseBlobOptions.MaxBlobSizeBytes));
    }
}
