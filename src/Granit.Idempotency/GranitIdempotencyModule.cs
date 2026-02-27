using Granit.Core.Modularity;
using Granit.Idempotency.Extensions;

namespace Granit.Idempotency;

/// <summary>
/// Granit module for HTTP idempotency middleware.
/// Registers <see cref="Abstractions.IIdempotencyStore"/>, <see cref="Internal.IdempotencyMiddleware"/>,
/// and all required dependencies from configuration section <c>"Idempotency"</c>.
/// </summary>
public sealed class GranitIdempotencyModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIdempotency();
}
