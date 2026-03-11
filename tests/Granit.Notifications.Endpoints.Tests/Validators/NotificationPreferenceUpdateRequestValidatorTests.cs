using FluentValidation.Results;
using Granit.Notifications.Endpoints.Dtos;
using Granit.Notifications.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Endpoints.Tests.Validators;

public sealed class NotificationPreferenceUpdateRequestValidatorTests
{
    private readonly NotificationPreferenceUpdateRequestValidator _validator = new();

    private static NotificationPreferenceUpdateRequest ValidRequest() => new()
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
        NotificationPreferenceUpdateRequest request = ValidRequest() with { NotificationTypeName = typeName! };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationPreferenceUpdateRequest.NotificationTypeName));
    }

    [Fact]
    public void Validate_NotificationTypeNameExceedsMaxLength_Fails()
    {
        string longName = new('x', NotificationPreferenceUpdateRequestValidator.MaxNotificationTypeNameLength + 1);
        NotificationPreferenceUpdateRequest request = ValidRequest() with { NotificationTypeName = longName };

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
        NotificationPreferenceUpdateRequest request = ValidRequest() with { ChannelName = channelName! };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(NotificationPreferenceUpdateRequest.ChannelName));
    }

    [Fact]
    public void Validate_ChannelNameExceedsMaxLength_Fails()
    {
        string longName = new('x', NotificationPreferenceUpdateRequestValidator.MaxChannelNameLength + 1);
        NotificationPreferenceUpdateRequest request = ValidRequest() with { ChannelName = longName };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // Multiple errors
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_MultipleInvalidFields_ReturnsAllErrors()
    {
        NotificationPreferenceUpdateRequest request = ValidRequest() with
        {
            NotificationTypeName = "",
            ChannelName = "",
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThanOrEqualTo(2);
    }
}
