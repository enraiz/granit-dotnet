using Microsoft.Extensions.Options;

namespace Granit.Wolverine.Internal;

/// <summary>
/// Validates <see cref="WolverineMessagingOptions"/> at startup.
/// </summary>
internal sealed class WolverineMessagingOptionsValidator : IValidateOptions<WolverineMessagingOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, WolverineMessagingOptions options)
    {
        List<string> failures = [];

        if (options.MaxRetryAttempts < 1)
        {
            failures.Add($"{nameof(options.MaxRetryAttempts)} must be ≥ 1.");
        }

        if (options.RetryDelays is not { Length: > 0 })
        {
            failures.Add($"{nameof(options.RetryDelays)} must contain at least one delay.");
        }
        else if (options.RetryDelays.Any(d => d <= TimeSpan.Zero))
        {
            failures.Add($"All {nameof(options.RetryDelays)} values must be positive.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
