using FluentValidation;
using Granit.Notifications.Endpoints.Dtos;
using Granit.Validation;

namespace Granit.Notifications.Endpoints.Validators;

/// <summary>
/// Validates the <see cref="NotificationPreferenceUpdateRequest"/> body for notification preference updates.
/// </summary>
/// <remarks>
/// MaxLength values must match <c>NotificationPreferenceConfiguration</c>:
/// NotificationTypeName = 256, ChannelName = 64.
/// </remarks>
internal sealed class NotificationPreferenceUpdateRequestValidator : GranitValidator<NotificationPreferenceUpdateRequest>
{
    /// <summary>Maximum length for notification type name (must match <c>NotificationPreferenceConfiguration</c>).</summary>
    internal const int MaxNotificationTypeNameLength = 256;

    /// <summary>Maximum length for channel name (must match <c>NotificationPreferenceConfiguration</c>).</summary>
    internal const int MaxChannelNameLength = 64;

    public NotificationPreferenceUpdateRequestValidator()
    {
        RuleFor(x => x.NotificationTypeName)
            .NotEmpty()
            .MaximumLength(MaxNotificationTypeNameLength);

        RuleFor(x => x.ChannelName)
            .NotEmpty()
            .MaximumLength(MaxChannelNameLength);
    }
}
