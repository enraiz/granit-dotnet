using FluentValidation;
using Granit.Templating.Endpoints.Dtos;
using Granit.Validation;

namespace Granit.Templating.Endpoints.Validators;

/// <summary>
/// Validates the <see cref="SaveTemplateCategoryRequest"/> body
/// for the POST/PUT template category endpoints.
/// </summary>
internal sealed class SaveTemplateCategoryRequestValidator : GranitValidator<SaveTemplateCategoryRequest>
{
    public SaveTemplateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.Icon)
            .MaximumLength(100)
            .When(x => x.Icon is not null);
    }
}
