using Granit.Vault.Aws.Options;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Vault.Aws.Tests;

public sealed class AwsVaultOptionsValidatorTests
{
    private readonly AwsVaultOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidOptions_ReturnsSuccess()
    {
        AwsVaultOptions options = new()
        {
            Region = "eu-west-1",
            KmsKeyId = "alias/granit-encryption",
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyRegion_ReturnsFail(string? region)
    {
        AwsVaultOptions options = new()
        {
            Region = region!,
            KmsKeyId = "alias/key",
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Region");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyKmsKeyId_ReturnsFail(string? keyId)
    {
        AwsVaultOptions options = new()
        {
            Region = "eu-west-1",
            KmsKeyId = keyId!,
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("KmsKeyId");
    }

    [Fact]
    public void Validate_RotationIntervalLessThanOne_ReturnsFail()
    {
        AwsVaultOptions options = new()
        {
            Region = "eu-west-1",
            KmsKeyId = "alias/key",
            RotationCheckIntervalMinutes = 0,
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("RotationCheckIntervalMinutes");
    }

    [Fact]
    public void Validate_TimeoutLessThanOne_ReturnsFail()
    {
        AwsVaultOptions options = new()
        {
            Region = "eu-west-1",
            KmsKeyId = "alias/key",
            TimeoutSeconds = 0,
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("TimeoutSeconds");
    }

    [Fact]
    public void Validate_AccessKeyWithoutSecret_ReturnsFail()
    {
        AwsVaultOptions options = new()
        {
            Region = "eu-west-1",
            KmsKeyId = "alias/key",
            AccessKeyId = "AKID",
            SecretAccessKey = null,
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("SecretAccessKey");
    }

    [Fact]
    public void Validate_SecretWithoutAccessKey_ReturnsFail()
    {
        AwsVaultOptions options = new()
        {
            Region = "eu-west-1",
            KmsKeyId = "alias/key",
            AccessKeyId = null,
            SecretAccessKey = "secret",
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("AccessKeyId");
    }

    [Fact]
    public void Validate_BothCredentials_ReturnsSuccess()
    {
        AwsVaultOptions options = new()
        {
            Region = "eu-west-1",
            KmsKeyId = "alias/key",
            AccessKeyId = "AKID",
            SecretAccessKey = "secret",
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_NeitherCredentials_ReturnsSuccess()
    {
        AwsVaultOptions options = new()
        {
            Region = "eu-west-1",
            KmsKeyId = "alias/key",
            AccessKeyId = null,
            SecretAccessKey = null,
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }
}
