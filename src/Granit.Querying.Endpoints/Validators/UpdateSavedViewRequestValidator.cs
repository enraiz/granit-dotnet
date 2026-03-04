using FluentValidation;
using Granit.Querying.SavedViews;
using Granit.Validation;

namespace Granit.Querying.Endpoints.Validators;

/// <summary>
/// Validates the <see cref="UpdateSavedViewRequest"/> body for saved view updates.
/// </summary>
internal sealed class UpdateSavedViewRequestValidator : GranitValidator<UpdateSavedViewRequest>
{
    internal const int MaxNameLength = 200;

    public UpdateSavedViewRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(MaxNameLength);
    }
}
