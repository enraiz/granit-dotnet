using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Exceptions;
using Granit.Webhooks.Internal;
using Granit.Webhooks.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Wolverine;
using Wolverine.ErrorHandling;

namespace Granit.Webhooks.Extensions;

/// <summary>
/// Extension methods for registering Granit.Webhooks services.
/// </summary>
public static class WebhooksHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Granit webhook dispatch engine.
    /// </summary>
    /// <remarks>
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="IWebhookPublisher"/> — application façade (scoped).</item>
    ///   <item><see cref="IWebhookSubscriptionStore"/> — InMemory by default (replaceable via <c>AddGranitWebhooksEntityFrameworkCore()</c>).</item>
    ///   <item><see cref="IWebhookDeliveryStore"/> — no-op by default (replaceable via <c>AddGranitWebhooksEntityFrameworkCore()</c>).</item>
    ///   <item><see cref="IWebhookSecretProtector"/> — pass-through by default (replaceable for production Vault integration).</item>
    ///   <item>Named <see cref="System.Net.Http.HttpClient"/> for webhook delivery with configurable timeout.</item>
    ///   <item>Wolverine local queue <c>webhook-delivery</c> with configurable parallelism.</item>
    ///   <item>Wolverine retry policy for <see cref="WebhookDeliveryException"/> (6 levels of exponential backoff).</item>
    /// </list>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Optional delegate to override <see cref="WebhooksOptions"/> values.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitWebhooks(
        this IHostApplicationBuilder builder,
        Action<WebhooksOptions>? configure = null)
    {
        // Bind and validate options at startup.
        builder.Services
            .AddOptions<WebhooksOptions>()
            .BindConfiguration(WebhooksOptions.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<WebhooksOptions>, WebhooksOptionsValidator>();

        // Read options directly from IConfiguration — DI container not yet built inside UseWolverine().
        WebhooksOptions options = new();
        builder.Configuration
            .GetSection(WebhooksOptions.SectionName)
            .Bind(options);
        configure?.Invoke(options);

        // Named HttpClient for webhook delivery — strict timeout to avoid blocking Wolverine workers.
        builder.Services.AddHttpClient(WebhooksConstants.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
            client.DefaultRequestHeaders.Add(
                "User-Agent", $"Granit-Webhooks/{WebhooksConstants.ApiVersion}");
        });

        // Default (replaceable) store registrations.
        builder.Services.AddSingleton<IWebhookSubscriptionStore, InMemoryWebhookSubscriptionStore>();
        builder.Services.AddScoped<IWebhookDeliveryStore, NullWebhookDeliveryStore>();
        builder.Services.AddSingleton<IWebhookSecretProtector, NoOpWebhookSecretProtector>();

        // Application façade.
        builder.Services.AddScoped<IWebhookPublisher, WolverineWebhookPublisher>();

        builder.UseWolverine(opts =>
        {
            // Dedicated local queue for HTTP delivery — isolated from the main bus.
            opts.LocalQueueFor<SendWebhookCommand>()
                .Named(WebhooksConstants.DeliveryQueueName)
                .MaximumParallelMessages(options.MaxParallelDeliveries);

            // Exponential backoff for retriable HTTP errors.
            // Declared before OnAnyException() to take priority.
            // After 6 retries (~14h30 total), Wolverine moves the message to the Dead-Letter Queue.
            opts.OnException<WebhookDeliveryException>()
                .RetryWithCooldown(
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromMinutes(2),
                    TimeSpan.FromMinutes(10),
                    TimeSpan.FromMinutes(30),
                    TimeSpan.FromHours(2),
                    TimeSpan.FromHours(12));
        });

        return builder;
    }
}
