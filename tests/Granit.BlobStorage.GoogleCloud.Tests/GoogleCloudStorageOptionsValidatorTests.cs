using Granit.BlobStorage.GoogleCloud.Options;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.GoogleCloud.Tests;

public sealed class GoogleCloudStorageOptionsValidatorTests
{
    private static readonly GoogleCloudStorageOptionsValidator Validator = new();

    private static GoogleCloudStorageOptions ValidOptions() => new()
    {
        ProjectId = "my-project-123",
        DefaultBucket = "granit-blobs",
    };

    [Fact]
    public void Validate_AllRequiredFieldsPresent_ReturnsSuccess()
    {
        ValidateOptionsResult result = Validator.Validate(null, ValidOptions());

        result.Failed.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingProjectId_ReturnsFail(string projectId)
    {
        GoogleCloudStorageOptions options = ValidOptions();
        options.ProjectId = projectId;

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(GoogleCloudStorageOptions.ProjectId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingDefaultBucket_ReturnsFail(string bucket)
    {
        GoogleCloudStorageOptions options = ValidOptions();
        options.DefaultBucket = bucket;

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(GoogleCloudStorageOptions.DefaultBucket));
    }

    [Fact]
    public void Validate_WithCredentialFilePath_ReturnsSuccess()
    {
        GoogleCloudStorageOptions options = ValidOptions();
        options.CredentialFilePath = "/secrets/sa-key.json";

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WithoutCredentialFilePath_UsesAdc_ReturnsSuccess()
    {
        GoogleCloudStorageOptions options = ValidOptions();
        options.CredentialFilePath = null;

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeFalse();
    }
}
