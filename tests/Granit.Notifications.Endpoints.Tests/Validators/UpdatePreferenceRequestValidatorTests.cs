using FluentValidation.Results;
using Granit.Notifications.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Endpoints.Tests.Validators;

public sealed class UpdatePreferenceRequestValidatorTests
{
    private readonly UpdatePreferenceRequestValidator _validator = new();

    private static UpdatePreferenceRequest ValidRequest() => new()
    {
        NotificationTypeName = "Order.Shipped",
        ChannelName = "Email",
        IsEnabled = true,
    };

    // -------------------------------------------------------------------------
    // Valid request
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_ValidRequest_ReturnsValid()
    {
        ValidationResult result = _validator.Validate(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // NotificationTypeName
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyNotificationTypeName_Fails(string? typeName)
    {
        UpdatePreferenceRequest request = ValidRequest() with { NotificationTypeName = typeName! };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdatePreferenceRequest.NotificationTypeName));
    }

    [Fact]
    public void Validate_NotificationTypeNameExceedsMaxLength_Fails()
    {
        string longName = new('x', UpdatePreferenceRequestValidator.MaxNotificationTypeNameLength + 1);
        UpdatePreferenceRequest request = ValidRequest() with { NotificationTypeName = longName };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // ChannelName
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyChannelName_Fails(string? channelName)
    {
        UpdatePreferenceRequest request = ValidRequest() with { ChannelName = channelName! };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdatePreferenceRequest.ChannelName));
    }

    [Fact]
    public void Validate_ChannelNameExceedsMaxLength_Fails()
    {
        string longName = new('x', UpdatePreferenceRequestValidator.MaxChannelNameLength + 1);
        UpdatePreferenceRequest request = ValidRequest() with { ChannelName = longName };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // Multiple errors
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_MultipleInvalidFields_ReturnsAllErrors()
    {
        UpdatePreferenceRequest request = ValidRequest() with
        {
            NotificationTypeName = "",
            ChannelName = "",
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThanOrEqualTo(2);
    }
}
