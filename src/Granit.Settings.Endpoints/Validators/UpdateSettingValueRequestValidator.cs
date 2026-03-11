using FluentValidation;
using Granit.Settings.Endpoints.Dtos;
using Granit.Validation;

namespace Granit.Settings.Endpoints.Validators;

/// <summary>
/// Validates <see cref="UpdateSettingValueRequest"/>.
/// </summary>
internal sealed class UpdateSettingValueRequestValidator : GranitValidator<UpdateSettingValueRequest>
{
    internal const int MaxValueLength = 4000;

    public UpdateSettingValueRequestValidator()
    {
        RuleFor(x => x.Value)
            .MaximumLength(MaxValueLength)
            .When(x => x.Value is not null);
    }
}
