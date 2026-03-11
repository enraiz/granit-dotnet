// =============================================================================
// Tests - WebhooksOptionsValidator
// =============================================================================
// Verifies boundary validation for HttpTimeoutSeconds and MaxParallelDeliveries.
// =============================================================================

using Granit.Webhooks.Options;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Webhooks.Tests;

public sealed class WebhooksOptionsValidatorTests
{
    private readonly WebhooksOptionsValidator _validator = new();

    // -------------------------------------------------------------------------
    // Valid options
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var options = new WebhooksOptions();

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(60)]
    [InlineData(120)]
    public void Validate_ValidHttpTimeoutSeconds_Succeeds(int timeout)
    {
        var options = new WebhooksOptions { HttpTimeoutSeconds = timeout };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_ValidMaxParallelDeliveries_Succeeds(int parallelism)
    {
        var options = new WebhooksOptions { MaxParallelDeliveries = parallelism };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // Invalid HttpTimeoutSeconds
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Validate_HttpTimeoutSecondsTooLow_Fails(int timeout)
    {
        var options = new WebhooksOptions { HttpTimeoutSeconds = timeout };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("HttpTimeoutSeconds");
    }

    [Theory]
    [InlineData(121)]
    [InlineData(300)]
    public void Validate_HttpTimeoutSecondsTooHigh_Fails(int timeout)
    {
        var options = new WebhooksOptions { HttpTimeoutSeconds = timeout };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("HttpTimeoutSeconds");
    }

    // -------------------------------------------------------------------------
    // Invalid MaxParallelDeliveries
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_MaxParallelDeliveriesTooLow_Fails(int parallelism)
    {
        var options = new WebhooksOptions { MaxParallelDeliveries = parallelism };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("MaxParallelDeliveries");
    }

    [Theory]
    [InlineData(101)]
    [InlineData(500)]
    public void Validate_MaxParallelDeliveriesTooHigh_Fails(int parallelism)
    {
        var options = new WebhooksOptions { MaxParallelDeliveries = parallelism };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("MaxParallelDeliveries");
    }

    // -------------------------------------------------------------------------
    // Both invalid
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_BothInvalid_ReportsMultipleErrors()
    {
        var options = new WebhooksOptions
        {
            HttpTimeoutSeconds = 0,
            MaxParallelDeliveries = 0,
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("HttpTimeoutSeconds");
        result.FailureMessage.ShouldContain("MaxParallelDeliveries");
    }
}
