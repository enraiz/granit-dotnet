using FluentValidation;
using Granit.Validation;

namespace Granit.Notifications.Endpoints.Validators;

/// <summary>
/// Validates the <see cref="UpdatePreferenceRequest"/> body for notification preference updates.
/// </summary>
/// <remarks>
/// MaxLength values must match <c>NotificationPreferenceConfiguration</c>:
/// NotificationTypeName = 256, ChannelName = 64.
/// </remarks>
internal sealed class UpdatePreferenceRequestValidator : GranitValidator<UpdatePreferenceRequest>
{
    /// <summary>Maximum length for notification type name (must match <c>NotificationPreferenceConfiguration</c>).</summary>
    internal const int MaxNotificationTypeNameLength = 256;

    /// <summary>Maximum length for channel name (must match <c>NotificationPreferenceConfiguration</c>).</summary>
    internal const int MaxChannelNameLength = 64;

    public UpdatePreferenceRequestValidator()
    {
        RuleFor(x => x.NotificationTypeName)
            .NotEmpty()
            .MaximumLength(MaxNotificationTypeNameLength);

        RuleFor(x => x.ChannelName)
            .NotEmpty()
            .MaximumLength(MaxChannelNameLength);
    }
}
