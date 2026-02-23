using Microsoft.Extensions.Options;

namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Validates <see cref="BackgroundJobsOptions"/> at startup.
/// </summary>
internal sealed class BackgroundJobsOptionsValidator : IValidateOptions<BackgroundJobsOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, BackgroundJobsOptions options)
    {
        if (options.Mode == JobStoreMode.Durable
            && string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.ConnectionString)} must be non-empty when " +
                $"{nameof(options.Mode)} is {nameof(JobStoreMode.Durable)}. " +
                "Provide a valid SQL Server or PostgreSQL connection string.");
        }

        return ValidateOptionsResult.Success;
    }
}
