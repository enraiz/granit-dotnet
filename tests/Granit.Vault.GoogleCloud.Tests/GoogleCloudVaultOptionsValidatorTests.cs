using Granit.Vault.GoogleCloud.Options;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Vault.GoogleCloud.Tests;

public sealed class GoogleCloudVaultOptionsValidatorTests
{
    private readonly GoogleCloudVaultOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidOptions_ReturnsSuccess()
    {
        GoogleCloudVaultOptions options = new()
        {
            ProjectId = "my-project",
            Location = "europe-west1",
            KeyRing = "granit-keyring",
            CryptoKey = "granit-key",
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyProjectId_ReturnsFail(string? projectId)
    {
        GoogleCloudVaultOptions options = new()
        {
            ProjectId = projectId!,
            Location = "global",
            KeyRing = "kr",
            CryptoKey = "ck",
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("ProjectId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyLocation_ReturnsFail(string? location)
    {
        GoogleCloudVaultOptions options = new()
        {
            ProjectId = "my-project",
            Location = location!,
            KeyRing = "kr",
            CryptoKey = "ck",
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Location");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyKeyRing_ReturnsFail(string? keyRing)
    {
        GoogleCloudVaultOptions options = new()
        {
            ProjectId = "my-project",
            Location = "global",
            KeyRing = keyRing!,
            CryptoKey = "ck",
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("KeyRing");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyCryptoKey_ReturnsFail(string? cryptoKey)
    {
        GoogleCloudVaultOptions options = new()
        {
            ProjectId = "my-project",
            Location = "global",
            KeyRing = "kr",
            CryptoKey = cryptoKey!,
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("CryptoKey");
    }

    [Fact]
    public void Validate_RotationIntervalLessThanOne_ReturnsFail()
    {
        GoogleCloudVaultOptions options = new()
        {
            ProjectId = "my-project",
            Location = "global",
            KeyRing = "kr",
            CryptoKey = "ck",
            RotationCheckIntervalMinutes = 0,
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("RotationCheckIntervalMinutes");
    }

    [Fact]
    public void Validate_TimeoutLessThanOne_ReturnsFail()
    {
        GoogleCloudVaultOptions options = new()
        {
            ProjectId = "my-project",
            Location = "global",
            KeyRing = "kr",
            CryptoKey = "ck",
            TimeoutSeconds = 0,
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("TimeoutSeconds");
    }
}
