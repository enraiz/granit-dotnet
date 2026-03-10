using Granit.Wolverine.Postgresql.Options;
using Microsoft.Extensions.Options;

namespace Granit.Wolverine.Postgresql.Internal;

/// <summary>
/// Validates <see cref="WolverinePostgresqlOptions"/> at startup.
/// </summary>
internal sealed class WolverinePostgresqlOptionsValidator : IValidateOptions<WolverinePostgresqlOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, WolverinePostgresqlOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TransportConnectionString))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.TransportConnectionString)} must be non-empty. " +
                "A valid PostgreSQL connection string is required for the Wolverine Outbox (HDS compliance).");
        }

        return ValidateOptionsResult.Success;
    }
}
