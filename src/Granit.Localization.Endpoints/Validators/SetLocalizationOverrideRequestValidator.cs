using FluentValidation;
using Granit.Localization.Endpoints.Dtos;
using Granit.Validation;

namespace Granit.Localization.Endpoints.Validators;

/// <summary>
/// Validates the <see cref="SetLocalizationOverrideRequest"/> body
/// for the PUT localization override endpoint.
/// </summary>
internal sealed class SetLocalizationOverrideRequestValidator : GranitValidator<SetLocalizationOverrideRequest>
{
    /// <summary>Maximum length for the override value (must match <c>LocalizationOverrideConfiguration</c>).</summary>
    internal const int MaxValueLength = 4000;

    public SetLocalizationOverrideRequestValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .MaximumLength(MaxValueLength);
    }
}
