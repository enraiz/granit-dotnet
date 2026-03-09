using FluentValidation;
using Granit.Identity.Endpoints.Dtos;
using Granit.Validation;

namespace Granit.Identity.Endpoints.Validators;

/// <summary>
/// Validates the <see cref="IdentityUserCacheListRequest"/> query parameters for listing cached users.
/// </summary>
internal sealed class IdentityUserCacheListRequestValidator : GranitValidator<IdentityUserCacheListRequest>
{
    /// <summary>Maximum length for the search filter.</summary>
    internal const int MaxSearchLength = 200;

    /// <summary>Maximum page size to prevent excessive memory usage.</summary>
    internal const int MaxPageSize = 100;

    public IdentityUserCacheListRequestValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(MaxSearchLength)
            .When(x => x.Search is not null);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}
