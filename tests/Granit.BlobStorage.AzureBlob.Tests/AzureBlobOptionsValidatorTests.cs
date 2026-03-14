using Granit.BlobStorage.AzureBlob.Options;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.AzureBlob.Tests;

public sealed class AzureBlobOptionsValidatorTests
{
    private static readonly AzureBlobOptionsValidator Validator = new();

    private static AzureBlobOptions ValidConnectionStringOptions() => new()
    {
        ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
        DefaultContainer = "granit-blobs",
        UseManagedIdentity = false,
    };

    private static AzureBlobOptions ValidManagedIdentityOptions() => new()
    {
        UseManagedIdentity = true,
        ServiceUri = new Uri("https://myaccount.blob.core.windows.net"),
        DefaultContainer = "granit-blobs",
    };

    // ── Connection string mode ──────────────────────────────────────────────

    [Fact]
    public void Validate_ConnectionString_AllFieldsPresent_ReturnsSuccess()
    {
        ValidateOptionsResult result = Validator.Validate(null, ValidConnectionStringOptions());

        result.Failed.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ConnectionString_MissingConnectionString_ReturnsFail(string connectionString)
    {
        AzureBlobOptions options = ValidConnectionStringOptions();
        options.ConnectionString = connectionString;

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(AzureBlobOptions.ConnectionString));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ConnectionString_MissingDefaultContainer_ReturnsFail(string container)
    {
        AzureBlobOptions options = ValidConnectionStringOptions();
        options.DefaultContainer = container;

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(AzureBlobOptions.DefaultContainer));
    }

    // ── Managed Identity mode ───────────────────────────────────────────────

    [Fact]
    public void Validate_ManagedIdentity_AllFieldsPresent_ReturnsSuccess()
    {
        ValidateOptionsResult result = Validator.Validate(null, ValidManagedIdentityOptions());

        result.Failed.ShouldBeFalse();
    }

    [Fact]
    public void Validate_ManagedIdentity_MissingServiceUri_ReturnsFail()
    {
        AzureBlobOptions options = ValidManagedIdentityOptions();
        options.ServiceUri = null;

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(AzureBlobOptions.ServiceUri));
    }

    [Fact]
    public void Validate_ManagedIdentity_MissingDefaultContainer_ReturnsFail()
    {
        AzureBlobOptions options = ValidManagedIdentityOptions();
        options.DefaultContainer = "";

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(nameof(AzureBlobOptions.DefaultContainer));
    }

    // ── Azurite (development) ───────────────────────────────────────────────

    [Fact]
    public void Validate_AzuriteConnectionString_ReturnsSuccess()
    {
        AzureBlobOptions options = ValidConnectionStringOptions();
        options.ConnectionString = "UseDevelopmentStorage=true";

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.ShouldBeFalse();
    }
}
