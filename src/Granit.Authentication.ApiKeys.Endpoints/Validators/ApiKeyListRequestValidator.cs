using FluentValidation;
using Granit.Authentication.ApiKeys.Endpoints.Dtos;
using Granit.Validation;

namespace Granit.Authentication.ApiKeys.Endpoints.Validators;

/// <summary>
/// Validates <see cref="ApiKeyListRequest"/> query parameters.
/// </summary>
internal sealed class ApiKeyListRequestValidator : GranitValidator<ApiKeyListRequest>
{
    internal const int MaxSearchLength = 200;
    internal const int MaxPageSize = 100;

    public ApiKeyListRequestValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(MaxSearchLength)
            .When(x => x.Search is not null);

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}
