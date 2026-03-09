using FluentValidation;
using Granit.Authentication.ApiKeys.Endpoints.Dtos;
using Granit.Validation;

namespace Granit.Authentication.ApiKeys.Endpoints.Validators;

/// <summary>
/// Validates <see cref="ApiKeyCreateRequest"/>.
/// </summary>
internal sealed class ApiKeyCreateRequestValidator : GranitValidator<ApiKeyCreateRequest>
{
    internal const int MaxNameLength = 200;
    internal const int MaxPermissions = 100;
    internal const int MaxCidrs = 50;

    public ApiKeyCreateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(MaxNameLength);

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Environment)
            .NotEmpty()
            .Must(env => env is "live" or "test" or "dev")
            .WithMessage("Environment must be 'live', 'test', or 'dev'.");

        RuleFor(x => x.Permissions)
            .Must(p => p is null || p.Count <= MaxPermissions)
            .WithMessage($"Maximum {MaxPermissions} permissions allowed.");

        RuleForEach(x => x.Permissions)
            .NotEmpty()
            .When(x => x.Permissions is { Count: > 0 });

        RuleFor(x => x.AllowedCidrs)
            .Must(c => c is null || c.Count <= MaxCidrs)
            .WithMessage($"Maximum {MaxCidrs} CIDR ranges allowed.");

        RuleForEach(x => x.AllowedCidrs)
            .NotEmpty()
            .Must(CidrValidator.IsValidCidr)
            .WithMessage("'{PropertyValue}' is not a valid CIDR notation.")
            .When(x => x.AllowedCidrs is { Count: > 0 });

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration date must be in the future.");

        RuleFor(x => x.CacheBehavior)
            .IsInEnum();
    }
}
