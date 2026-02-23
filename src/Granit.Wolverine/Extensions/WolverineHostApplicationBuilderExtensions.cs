using Granit.Wolverine.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Wolverine;
using Wolverine.ErrorHandling;

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
    ///   <item>Retry policy from <see cref="WolverineMessagingOptions"/> (default: 5 s / 30 s / 5 min).</item>
    ///   <item>No Outbox — add a provider module (e.g., <c>AddGranitWolverineWithPostgresql()</c>).</item>
    /// </list>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Optional additional Wolverine configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitWolverine(
        this IHostApplicationBuilder builder,
        Action<WolverineOptions>? configure = null)
    {
        // Bind and validate options at startup via DI.
        builder.Services
            .AddOptions<WolverineMessagingOptions>()
            .BindConfiguration(WolverineMessagingOptions.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<WolverineMessagingOptions>,
            WolverineMessagingOptionsValidator>();

        // Read options directly from IConfiguration: the DI container is not yet
        // built at this point, so IOptions<> is not resolvable inside UseWolverine().
        WolverineMessagingOptions messagingOptions = new();
        builder.Configuration
            .GetSection(WolverineMessagingOptions.SectionName)
            .Bind(messagingOptions);

        builder.UseWolverine(opts =>
        {
            // IDomainEvent — force local routing, never forward to external transports.
            // IIntegrationEvent routing is configured by the provider package.
            opts.PublishMessage<Core.Events.IDomainEvent>()
                .ToLocalQueue("domain-events");

            // Global retry policy — applied to all unhandled exceptions.
            TimeSpan[] delays = messagingOptions.RetryDelays
                .Take(messagingOptions.MaxRetryAttempts)
                .ToArray();

            opts.OnAnyException().RetryWithCooldown(delays);

            configure?.Invoke(opts);
        });

        return builder;
    }
}
