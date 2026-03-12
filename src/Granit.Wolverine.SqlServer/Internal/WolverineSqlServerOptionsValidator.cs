using Granit.Wolverine.SqlServer.Options;
using Microsoft.Extensions.Options;

namespace Granit.Wolverine.SqlServer.Internal;

/// <summary>
/// Validates <see cref="WolverineSqlServerOptions"/> at startup.
/// </summary>
internal sealed class WolverineSqlServerOptionsValidator : IValidateOptions<WolverineSqlServerOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, WolverineSqlServerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TransportConnectionString))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.TransportConnectionString)} must be non-empty. " +
                "A valid SQL Server connection string is required for the Wolverine Outbox (ISO 27001 compliance).");
        }

        return ValidateOptionsResult.Success;
    }
}
