using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Idempotency.Extensions;
using DigitalDynamics.Foundation.Idempotency.Models;

namespace DigitalDynamics.Foundation.Idempotency;

/// <summary>
/// Foundation module for HTTP idempotency middleware.
/// Registers <see cref="Abstractions.IIdempotencyStore"/>, <see cref="Internal.IdempotencyMiddleware"/>,
/// and all required dependencies from configuration section <c>"Idempotency"</c>.
/// </summary>
public sealed class FoundationIdempotencyModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationIdempotency(
            context.Configuration.GetSection(IdempotencyOptions.SectionName));
}
