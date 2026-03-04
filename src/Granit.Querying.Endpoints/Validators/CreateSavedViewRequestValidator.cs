using FluentValidation;
using Granit.Querying.SavedViews;
using Granit.Validation;

namespace Granit.Querying.Endpoints.Validators;

/// <summary>
/// Validates the <see cref="CreateSavedViewRequest"/> body for saved view creation.
/// </summary>
internal sealed class CreateSavedViewRequestValidator : GranitValidator<CreateSavedViewRequest>
{
    internal const int MaxNameLength = 200;

    public CreateSavedViewRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(MaxNameLength);
    }
}
