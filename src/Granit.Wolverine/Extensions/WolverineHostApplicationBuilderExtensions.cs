using Microsoft.Extensions.Hosting;
using Wolverine;

namespace Granit.Wolverine.Extensions;

/// <summary>
/// Extension methods for registering Granit Wolverine core services.
/// </summary>
public static class WolverineHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Granit Wolverine core infrastructure (provider-agnostic).
    /// </summary>
    /// <remarks>
    /// Configures Wolverine with:
    /// <list type="bullet">
    ///   <item>Local routing for <see cref="Granit.Core.Events.IDomainEvent"/> — never routed to external transports.</item>
    ///   <item>No Outbox configuration — add a provider module for persistence
    ///     (e.g., <c>AddGranitWolverineWithPostgresql()</c>).</item>
    /// </list>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Optional additional Wolverine configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitWolverine(
        this IHostApplicationBuilder builder,
        Action<WolverineOptions>? configure = null)
    {
        builder.UseWolverine(opts =>
        {
            // IDomainEvent — force local routing, never forward to external transports.
            // IIntegrationEvent routing is configured by the provider package.
            opts.PublishMessage<Core.Events.IDomainEvent>()
                .ToLocalQueue("domain-events");

            configure?.Invoke(opts);
        });

        return builder;
    }
}
